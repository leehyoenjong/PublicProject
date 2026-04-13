using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using BackEnd;
using LitJson;
using BACKND.Database;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 데이터베이스 + 유저 데이터 추상화.
    /// - 유저데이터(GameData) 저장/로드: 뒤끝 베이스 GameData API.
    /// - 유연 테이블 쿼리: BACKND.Database Client (Phase 11 부터 실 연결).
    ///   Client 는 `BackendConfig.DatabaseUuid` 로 생성하고 `Initialize()` 를 1회만 수행해 캐시한다.
    /// - 차트: `Backend.CDN.Content.Table.Get()` 까지만 (개별 payload 는 Phase 12+ 이관).
    /// </summary>
    public class BackendDatabase : IBackendDatabase, IDisposable
    {
        private const string ACTION_SAVE = "SaveUserData";
        private const string ACTION_LOAD = "LoadUserData";
        private const string ACTION_QUERY = "QueryFlexibleTable";
        private const string ACTION_CHART = "DownloadChart";

        private const string USER_DATA_TABLE = "USER_DATA";
        private const string USER_DATA_COLUMN = "json";

        private readonly BackendConfig _config;
        private readonly IEventBus _eventBus;

        private Client _client;
        private BTask _initTask;
        private bool _clientReady;

        public BackendDatabase(BackendConfig config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Dispose()
        {
            try
            {
                _client?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendDatabase] Client Dispose 예외: {e.Message}");
            }
            _client = null;
            _clientReady = false;
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

        public async void QueryFlexibleTable<T>(
            IFlexibleTableFilter filter,
            Action<bool, IReadOnlyList<T>, BackendError> onComplete) where T : BaseModel, new()
        {
            if (!await EnsureClientAsync())
            {
                DispatchQuery(onComplete, false, null, BackendError.NotInitialized);
                return;
            }

            int conditionCount = filter != null ? filter.Conditions.Count : 0;
            try
            {
                var builder = _client.From<T>();
                int applied = ApplyConditions<T>(ref builder, filter);
                Debug.Log($"[BackendDatabase] QueryFlexibleTable<{typeof(T).Name}>: Conditions applied={applied}/{conditionCount}");

                List<T> rows = await builder.ToList();
                DispatchQuery(onComplete, true, rows as IReadOnlyList<T>, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] QueryFlexibleTable 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NetworkError, e.Message);
                DispatchQuery(onComplete, false, null, BackendError.NetworkError);
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
                // Phase 10 방식: 1단계 Table.Get 만 수행, 2단계는 Phase 12+ 이관.
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
                Debug.Log($"[BackendDatabase] 차트 Table.Get 성공: requested={chartName}");
                callback?.Invoke(true, payload, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendDatabase] 차트 다운로드 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CHART, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, string.Empty, BackendError.NetworkError);
            }
        }

        /// <summary>
        /// BACKND.Database Client 를 1회 생성 + Initialize 하고 캐시한다.
        /// Bootstrapper 가 사전 호출해도 되고, QueryFlexibleTable 최초 호출 시 lazy 초기화된다.
        /// </summary>
        public async BTask<bool> EnsureClientAsync()
        {
            if (_clientReady) return true;

            string uuid = _config != null ? _config.DatabaseUuid : string.Empty;
            if (string.IsNullOrEmpty(uuid))
            {
                Debug.LogWarning("[BackendDatabase] DatabaseUuid 미설정 — Client 초기화 보류");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NotInitialized, "DatabaseUuid empty");
                return false;
            }

            if (_initTask != null)
            {
                try { await _initTask; }
                catch (Exception) { /* 실패는 아래에서 확인 */ }
                return _clientReady;
            }

            try
            {
                _client = new Client(uuid);
                _initTask = _client.Initialize();
                await _initTask;
                _clientReady = true;
                Debug.Log("[BackendDatabase] BACKND.Database Client 초기화 완료");
            }
            catch (Exception e)
            {
                _clientReady = false;
                Debug.LogError($"[BackendDatabase] Client 초기화 실패: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_QUERY, BackendError.NetworkError, e.Message);
            }

            return _clientReady;
        }

        private static int ApplyConditions<T>(ref QueryBuilder<T> builder, IFlexibleTableFilter filter)
            where T : BaseModel, new()
        {
            if (filter == null || filter.Conditions.Count == 0) return 0;

            int applied = 0;
            foreach (var cond in filter.Conditions)
            {
                var expr = BuildExpression<T>(cond);
                if (expr == null) continue;
                builder = builder.Where(expr);
                applied++;
            }
            return applied;
        }

        private static Expression<Func<T, bool>> BuildExpression<T>(FilterCondition cond)
            where T : BaseModel, new()
        {
            if (string.IsNullOrEmpty(cond.Column)) return null;
            try
            {
                var param = Expression.Parameter(typeof(T), "x");
                var prop = Expression.PropertyOrField(param, cond.Column);

                Expression body;
                switch (cond.Op)
                {
                    case FlexibleFilterOp.Eq:
                        body = Expression.Equal(prop, MakeConstant(cond.Value, prop.Type));
                        break;
                    case FlexibleFilterOp.Gt:
                        body = Expression.GreaterThan(prop, MakeConstant(cond.Value, prop.Type));
                        break;
                    case FlexibleFilterOp.Lt:
                        body = Expression.LessThan(prop, MakeConstant(cond.Value, prop.Type));
                        break;
                    case FlexibleFilterOp.In:
                        body = BuildInBody(prop, cond.Value);
                        if (body == null) return null;
                        break;
                    default:
                        return null;
                }

                return Expression.Lambda<Func<T, bool>>(body, param);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendDatabase] Expression 구성 실패: column={cond.Column}, op={cond.Op}, msg={e.Message}");
                return null;
            }
        }

        private static Expression MakeConstant(object value, Type targetType)
        {
            object converted = ConvertValue(value, targetType);
            return Expression.Constant(converted, targetType);
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (nonNullable.IsInstanceOfType(value)) return value;
            return Convert.ChangeType(value, nonNullable);
        }

        private static Expression BuildInBody(Expression prop, object raw)
        {
            if (!(raw is IEnumerable source)) return null;
            Type elementType = prop.Type;
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var v in source)
                list.Add(ConvertValue(v, elementType));

            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            MethodInfo containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);

            var listExpr = Expression.Constant(list, enumerableType);
            return Expression.Call(containsMethod, listExpr, prop);
        }

        private void DispatchQuery<T>(
            Action<bool, IReadOnlyList<T>, BackendError> onComplete,
            bool ok, IReadOnlyList<T> list, BackendError err)
        {
            if (onComplete == null) return;
            IReadOnlyList<T> safe = list ?? Array.Empty<T>();
            if (BackendMainThreadDispatcher.Instance != null)
                BackendMainThreadDispatcher.Instance.Enqueue(() => onComplete.Invoke(ok, safe, err));
            else
                onComplete.Invoke(ok, safe, err);
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
