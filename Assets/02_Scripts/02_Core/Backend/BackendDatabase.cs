using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using BackEnd;
using LitJson;
// NOTE: 뒤끝 데이터베이스 SDK(BACKND.Database) 는 별도 제품으로 기본 import 되어 있지 않을 수 있다.
//       컴파일 안전을 위해 'using BACKND.Database;' 사용 금지. 런타임 리플렉션으로 가드한다.

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 데이터베이스 + 유저 데이터 추상화.
    /// - 유저데이터(GameData) 저장/로드: 뒤끝 베이스 GameData API (Backend.dll 이미 import).
    /// - 유연 테이블 쿼리: BACKND.Database Client (리플렉션 기반 감지 + 호출).
    ///   SDK 미import 시 `QueryFlexibleTable` 은 `BackendError.NotInitialized` 로 즉시 실패 콜백하며,
    ///   다른 Backend 서비스는 정상 동작한다.
    ///   SDK 도입: 뒤끝 콘솔에서 다운로드 후 Assets/TheBackend/Plugins/ 에 BACKND.Database.dll 배치.
    /// </summary>
    public class BackendDatabase : IBackendDatabase
    {
        private const string ACTION_SAVE = "SaveUserData";
        private const string ACTION_LOAD = "LoadUserData";
        private const string ACTION_QUERY = "QueryFlexibleTable";
        private const string ACTION_CHART = "DownloadChart";

        private const string USER_DATA_TABLE = "USER_DATA";
        private const string USER_DATA_COLUMN = "json";

        private const string DB_CLIENT_TYPE = "BACKND.Database.Client, BACKND.Database";
        private const string DB_METHOD_DATABASE = "Database";
        private const string DB_METHOD_TO_LIST_ASYNC = "ToListAsync";
        private const string DB_PROP_INSTANCE = "Instance";

        private readonly BackendConfig _config;
        private readonly IEventBus _eventBus;

        private bool _clientChecked;
        private bool _clientAvailable;
        private Type _clientType;

        public BackendDatabase(BackendConfig config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void SaveUserData<T>(T data, Action<bool, BackendError, string> callback) where T : class
        {
            if (data == null)
            {
                Debug.LogWarning("[BackendDatabase] 저장 중단: data null");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SAVE, BackendError.InvalidRequest, "data null");
                callback?.Invoke(false, BackendError.InvalidRequest, "data null");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data);
                var param = new Param();
                param.Add(USER_DATA_COLUMN, json);

                var bro = Backend.GameData.Insert(USER_DATA_TABLE, param);
                var ok = bro.IsSuccess();
                Debug.Log($"[BackendDatabase] 유저데이터 저장: ok={ok}, code={bro.GetStatusCode()}");

                var err = BackendErrorMapper.Map(bro);
                if (ok) BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                else BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SAVE, err, bro.GetMessage());

                callback?.Invoke(ok, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] 저장 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SAVE, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void LoadUserData<T>(Action<bool, T, BackendError> callback) where T : class
        {
            try
            {
                var bro = Backend.GameData.GetMyData(USER_DATA_TABLE, new Where());
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[BackendDatabase] 로드 실패: code={bro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_LOAD, err, bro.GetMessage());
                    callback?.Invoke(false, null, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var parsed = ParseUserData<T>(bro);
                if (parsed == null)
                {
                    Debug.LogWarning("[BackendDatabase] 로드 성공 후 파싱 결과 없음");
                    callback?.Invoke(false, null, BackendError.NotFound);
                    return;
                }

                Debug.Log($"[BackendDatabase] 유저데이터 로드 성공: type={typeof(T).Name}");
                callback?.Invoke(true, parsed, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] 로드 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_LOAD, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, null, BackendError.NetworkError);
            }
        }

        public void QueryFlexibleTable(
            FlexibleTableKey key,
            IFlexibleTableFilter filter,
            Action<bool, IReadOnlyList<Dictionary<string, object>>, BackendError> onComplete)
        {
            string tableName = _config != null ? _config.GetFlexibleTableName(key) : string.Empty;
            if (string.IsNullOrEmpty(tableName))
            {
                Debug.LogWarning($"[BackendDatabase] QueryFlexibleTable 중단: 테이블명 미바인딩 key={key}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NotInitialized, "no table binding");
                onComplete?.Invoke(false, null, BackendError.NotInitialized);
                return;
            }

            if (!EnsureClient())
            {
                Debug.LogWarning("[BackendDatabase] BACKND.Database SDK not imported. Skipping QueryFlexibleTable.");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NotInitialized, "BACKND.Database SDK missing");
                onComplete?.Invoke(false, null, BackendError.NotInitialized);
                return;
            }

            int conditionCount = filter != null ? filter.Conditions.Count : 0;
            try
            {
                // BACKND.Database 공식 문서(/sdk-docs/database/intro/)의 실 사용 패턴:
                //   var db = new Client("UUID");
                //   await db.Initialize();
                //   var list = await db.From<T>().Where(Expression<Func<T, bool>>).Take(N).ToList();
                // → Client.Instance 싱글톤이 아닌 생성자 + Initialize 필요. UUID 출처가 BackendConfig 에 없어
                //   Phase 9 현재는 실 쿼리 호출을 보류하고, 타입 감지만 유지한다(회귀 방지).
                // 서버 Where (Expression<Func<T,bool>>) 매핑은 리플렉션으로 안전하게 호출하기 어려워
                // 전체 필터는 메모리 폴백을 유지한다.
                int serverWhere = 0;
                int memoryFilter = conditionCount; // 현재 전부 메모리 폴백.
                Debug.Log($"[BackendDatabase] ServerWhere:{serverWhere}, MemoryFilter:{memoryFilter} (table={tableName})");

                Debug.LogWarning("[BackendDatabase] QueryFlexibleTable: SDK Client 생성자/Initialize UUID 출처 미정 — 쿼리 호출 보류(메모리 필터 경로 스텁). Phase 10+ 이관.");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NotInitialized, "client uuid not bound");
                onComplete?.Invoke(false, new List<Dictionary<string, object>>(), BackendError.NotInitialized);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] QueryFlexibleTable 리플렉션 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.Unknown, e.Message);
                onComplete?.Invoke(false, new List<Dictionary<string, object>>(), BackendError.Unknown);
            }
        }

        public void DownloadChart(string chartName, Action<bool, string, BackendError> callback)
        {
            if (string.IsNullOrEmpty(chartName))
            {
                callback?.Invoke(false, string.Empty, BackendError.InvalidRequest);
                return;
            }

            try
            {
                // 뒤끝 SDK 차트 다운로드 — 공식 문서 /chart-table/ 기준.
                // NOTE: 2단계(Content.Get) 구현은 `ContentTableItem` FQN 이 프레임워크 참조 범위에서 해결되지 않아
                //       (CS0246, 문서에서도 FQN 미명시) Phase 11+ 로 이관한다.
                //       현재는 Table.Get() 전체 JSON payload 를 반환하며, 호출부가 chartName 으로 파싱한다.
                var tableBro = Backend.CDN.Content.Table.Get();
                if (!tableBro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(tableBro);
                    Debug.LogWarning($"[BackendDatabase] 차트 Table.Get 실패: code={tableBro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CHART, err, tableBro.GetMessage());
                    callback?.Invoke(false, string.Empty, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                string payload = tableBro.GetReturnValuetoJSON()?.ToJson() ?? string.Empty;
                Debug.Log($"[BackendDatabase] 차트 Table.Get 성공: requested={chartName} (2단계 Content.Get 은 Phase 11+ 이관)");
                callback?.Invoke(true, payload, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] 차트 다운로드 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CHART, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, string.Empty, BackendError.NetworkError);
            }
        }

        // Phase 10 QueryFlexibleTable 재활성 시 사용 — 현재 미호출 (A1 보류).
        private void DispatchQueryResult(
            Task completed,
            string tableName,
            IFlexibleTableFilter filter,
            Action<bool, IReadOnlyList<Dictionary<string, object>>, BackendError> onComplete,
            int conditionCount)
        {
            void Apply()
            {
                try
                {
                    if (completed.IsFaulted)
                    {
                        var baseEx = completed.Exception?.GetBaseException();
                        string msg = baseEx != null ? baseEx.Message : "task faulted";
                        Debug.LogError($"[BackendDatabase] QueryFlexibleTable 비동기 실패: {msg}");
                        BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NetworkError, msg);
                        onComplete?.Invoke(false, new List<Dictionary<string, object>>(), BackendError.NetworkError);
                        return;
                    }

                    var resultProp = completed.GetType().GetProperty("Result");
                    var raw = resultProp?.GetValue(completed) as IEnumerable<Dictionary<string, object>>;
                    var rows = raw != null
                        ? new List<Dictionary<string, object>>(raw)
                        : new List<Dictionary<string, object>>();

                    var filtered = ApplyMemoryFilter(rows, filter);
                    Debug.Log($"[BackendDatabase] QueryFlexibleTable: table={tableName}, conditions={conditionCount}, rows={filtered.Count}");
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                    onComplete?.Invoke(true, filtered, BackendError.None);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendDatabase] QueryFlexibleTable 결과 처리 예외: {e.Message}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.Unknown, e.Message);
                    onComplete?.Invoke(false, new List<Dictionary<string, object>>(), BackendError.Unknown);
                }
            }

            if (BackendMainThreadDispatcher.Instance != null)
                BackendMainThreadDispatcher.Instance.Enqueue(Apply);
            else
                Apply();
        }

        // Phase 10 QueryFlexibleTable 재활성 시 사용 — 현재 미호출 (A1 보류).
        private static List<Dictionary<string, object>> ApplyMemoryFilter(
            List<Dictionary<string, object>> rows, IFlexibleTableFilter filter)
        {
            if (filter == null || filter.Conditions.Count == 0) return rows;

            var result = new List<Dictionary<string, object>>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool pass = true;
                foreach (var cond in filter.Conditions)
                {
                    if (!row.TryGetValue(cond.Column, out var cellValue) || !MatchesCondition(cellValue, cond.Op, cond.Value))
                    {
                        pass = false;
                        break;
                    }
                }
                if (pass) result.Add(row);
            }
            return result;
        }

        // Phase 10 QueryFlexibleTable 재활성 시 사용 — 현재 미호출 (A1 보류).
        private static bool MatchesCondition(object cellValue, FlexibleFilterOp op, object condValue)
        {
            switch (op)
            {
                case FlexibleFilterOp.Eq:
                    return Equals(cellValue, condValue) ||
                           string.Equals(cellValue?.ToString(), condValue?.ToString(), StringComparison.Ordinal);
                case FlexibleFilterOp.Gt:
                    return CompareValues(cellValue, condValue) > 0;
                case FlexibleFilterOp.Lt:
                    return CompareValues(cellValue, condValue) < 0;
                case FlexibleFilterOp.In:
                    return ContainsInList(cellValue, condValue);
                default:
                    return false;
            }
        }

        // Phase 10 QueryFlexibleTable 재활성 시 사용 — 현재 미호출 (A1 보류).
        private static bool ContainsInList(object cellValue, object condValue)
        {
            if (!(condValue is IEnumerable<object> list)) return false;
            foreach (var item in list)
            {
                if (Equals(cellValue, item) ||
                    string.Equals(cellValue?.ToString(), item?.ToString(), StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        // Phase 10 QueryFlexibleTable 재활성 시 사용 — 현재 미호출 (A1 보류).
        private static int CompareValues(object a, object b)
        {
            if (a is IComparable comparable && b != null)
            {
                try { return comparable.CompareTo(b); }
                catch { /* fallback 아래 */ }
            }
            return string.Compare(a?.ToString() ?? string.Empty, b?.ToString() ?? string.Empty, StringComparison.Ordinal);
        }

        /// <summary>
        /// BACKND.Database.Client 타입 존재 여부를 런타임 리플렉션으로 1회 감지.
        /// SDK 미 import 환경에서도 컴파일 통과를 보장한다.
        /// </summary>
        private bool EnsureClient()
        {
            if (_clientChecked) return _clientAvailable;
            _clientChecked = true;

            try
            {
                _clientType = Type.GetType(DB_CLIENT_TYPE, throwOnError: false);
                _clientAvailable = _clientType != null;
                if (_clientAvailable)
                    Debug.Log("[BackendDatabase] BACKND.Database Client 타입 감지 완료");
            }
            catch (Exception e)
            {
                _clientAvailable = false;
                _clientType = null;
                Debug.LogError($"[BackendDatabase] Client 타입 감지 예외: {e.Message}");
            }

            return _clientAvailable;
        }

        private static T ParseUserData<T>(BackendReturnObject bro) where T : class
        {
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("rows")) return null;
                JsonData rows = json["rows"];
                if (rows == null || !rows.IsArray || rows.Count == 0) return null;

                JsonData firstRow = rows[0];
                if (firstRow == null || !firstRow.ContainsKey(USER_DATA_COLUMN)) return null;

                JsonData cell = firstRow[USER_DATA_COLUMN];
                string raw = ExtractCellString(cell);
                if (string.IsNullOrEmpty(raw)) return null;

                return JsonUtility.FromJson<T>(raw);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] 유저데이터 파싱 실패: {e.Message}");
                return null;
            }
        }

        private static string ExtractCellString(JsonData cell)
        {
            if (cell == null) return string.Empty;
            if (cell.IsString) return cell.ToString();
            if (cell.IsObject && cell.ContainsKey("S"))
            {
                var inner = cell["S"];
                return inner != null ? inner.ToString() : string.Empty;
            }
            return cell.ToString();
        }
    }
}
