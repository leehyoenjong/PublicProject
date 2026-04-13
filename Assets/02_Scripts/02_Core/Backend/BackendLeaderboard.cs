using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 유저 리더보드. 논리키(LeaderboardKey) 를 BackendConfig 에서 uuid 로 변환해 호출.
    /// 신규 SDK 네임스페이스: `Backend.Leaderboard.User` (구 ULeaderBoard/URank 대체, BRO 기반).
    /// 공식 문서:
    ///   - 갱신: /sdk-docs/backend/base/leaderboard/user/update/
    ///     `UpdateMyDataAndRefreshLeaderboard(uuid, tableName, rowIndate, Param)`
    ///   - 전체: /sdk-docs/backend/base/leaderboard/user/get-list/
    ///     `GetLeaderboard(uuid[, limit[, offset]])`
    ///   - 내/주변: /sdk-docs/backend/base/leaderboard/user/get-mine/
    ///     `GetMyLeaderboard(uuid)` 또는 `GetMyLeaderboard(uuid, gap)` — gap 은 ±N 랭커.
    /// </summary>
    public class BackendLeaderboard : IBackendLeaderboard
    {
        private const string ACTION_SUBMIT = "SubmitScore";
        private const string ACTION_GET_TOP = "GetTop";
        private const string ACTION_GET_MY_RANK = "GetMyRank";
        private const string ACTION_GET_AROUND = "GetAround";

        // 리더보드 연동 기본 테이블/컬럼. 프로젝트별 재정의가 필요하면 BackendConfig 에 노출하여 주입 가능.
        private const string DEFAULT_RANK_TABLE = "RANK_DATA";
        private const string DEFAULT_SCORE_COLUMN = "score";

        private readonly BackendConfig _config;
        private readonly IEventBus _eventBus;

        public BackendLeaderboard(BackendConfig config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void SubmitScore(LeaderboardKey key, long score, Action<bool, BackendError, string> callback)
        {
            if (!TryResolveUuid(key, ACTION_SUBMIT, out var uuid, out var resolveErr))
            {
                callback?.Invoke(false, resolveErr, "uuid not bound");
                return;
            }

            try
            {
                // 뒤끝 신규 API 는 GameData 테이블의 특정 row(=rowInDate) 를 갱신하고 리더보드를 refresh 한다.
                // 단순 점수 제출이라면: rowInDate = Backend.UserInDate (유저 자신의 소유 row) + 기본 RANK 테이블 사용.
                string rowInDate = Backend.UserInDate ?? string.Empty;
                var param = new Param();
                param.Add(DEFAULT_SCORE_COLUMN, score);

                var bro = Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard(uuid, DEFAULT_RANK_TABLE, rowInDate, param);
                var ok = bro.IsSuccess();
                Debug.Log($"[BackendLeaderboard] 점수 제출 key={key}, uuid={uuid}, score={score}, ok={ok}");

                var err = BackendErrorMapper.Map(bro);
                if (ok)
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                    _eventBus?.Publish(new BackendLeaderboardUpdatedEvent { Uuid = uuid });
                }
                else
                {
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SUBMIT, err, bro.GetMessage());
                }

                callback?.Invoke(ok, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendLeaderboard] 점수 제출 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SUBMIT, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void GetTop(LeaderboardKey key, int limit, Action<bool, IReadOnlyList<LeaderboardEntry>, BackendError> callback)
        {
            if (!TryResolveUuid(key, ACTION_GET_TOP, out var uuid, out var resolveErr))
            {
                callback?.Invoke(false, null, resolveErr);
                return;
            }

            try
            {
                // 신규 Backend.Leaderboard.User.GetLeaderboard(uuid, limit). limit 범위: 1~50.
                var bro = Backend.Leaderboard.User.GetLeaderboard(uuid, limit);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[BackendLeaderboard] 상위 조회 실패: code={bro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_TOP, err, bro.GetMessage());
                    callback?.Invoke(false, null, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var list = ParseEntries(bro);
                Debug.Log($"[BackendLeaderboard] 상위 {list.Count}건 조회 uuid={uuid}");
                _eventBus?.Publish(new BackendLeaderboardUpdatedEvent { Uuid = uuid });
                callback?.Invoke(true, list, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendLeaderboard] 상위 조회 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_TOP, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, null, BackendError.NetworkError);
            }
        }

        public void GetMyRank(LeaderboardKey key, Action<bool, LeaderboardEntry, BackendError> callback)
        {
            if (!TryResolveUuid(key, ACTION_GET_MY_RANK, out var uuid, out var resolveErr))
            {
                callback?.Invoke(false, default, resolveErr);
                return;
            }

            try
            {
                // 신규 GetMyLeaderboard(uuid) — 내 순위 1건만.
                var bro = Backend.Leaderboard.User.GetMyLeaderboard(uuid);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_MY_RANK, err, bro.GetMessage());
                    callback?.Invoke(false, default, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var list = ParseEntries(bro);
                var me = list.Count > 0 ? list[0] : default;
                _eventBus?.Publish(new BackendLeaderboardUpdatedEvent { Uuid = uuid });
                callback?.Invoke(true, me, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendLeaderboard] 내 순위 조회 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_MY_RANK, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, default, BackendError.NetworkError);
            }
        }

        public void GetAround(LeaderboardKey key, int range, Action<bool, IReadOnlyList<LeaderboardEntry>, BackendError> callback)
        {
            if (!TryResolveUuid(key, ACTION_GET_AROUND, out var uuid, out var resolveErr))
            {
                callback?.Invoke(false, null, resolveErr);
                return;
            }

            // 신규 GetMyLeaderboard(uuid, gap) — 내 순위 ±gap 랭커 포함. gap 범위 0~25.
            int gap = Math.Clamp(range, 0, 25);
            try
            {
                var bro = Backend.Leaderboard.User.GetMyLeaderboard(uuid, gap);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[BackendLeaderboard] 주변 순위 조회 실패: code={bro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_AROUND, err, bro.GetMessage());
                    callback?.Invoke(false, null, err);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                var list = ParseEntries(bro);
                _eventBus?.Publish(new BackendLeaderboardUpdatedEvent { Uuid = uuid });
                Debug.Log($"[BackendLeaderboard] 주변 순위 {list.Count}건 (gap={gap})");
                callback?.Invoke(true, list, BackendError.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendLeaderboard] 주변 순위 조회 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_AROUND, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, null, BackendError.NetworkError);
            }
        }

        private bool TryResolveUuid(LeaderboardKey key, string action, out string uuid, out BackendError error)
        {
            uuid = _config != null ? _config.GetLeaderboardUuid(key) : string.Empty;
            if (string.IsNullOrEmpty(uuid))
            {
                Debug.LogWarning($"[BackendLeaderboard] {action} 중단: uuid 미바인딩 key={key}");
                error = BackendError.NotInitialized;
                BackendEventDispatcher.PublishFailed(_eventBus, action, error, $"no uuid for {key}");
                return false;
            }
            error = BackendError.None;
            return true;
        }

        private List<LeaderboardEntry> ParseEntries(BackendReturnObject bro)
        {
            var list = new List<LeaderboardEntry>();
            try
            {
                JsonData json = bro.GetReturnValuetoJSON();
                if (json == null || !json.ContainsKey("rows")) return list;

                JsonData rows = json["rows"];
                if (rows == null || !rows.IsArray) return list;

                int count = rows.Count;
                for (int i = 0; i < count; i++)
                {
                    var row = rows[i];
                    var entry = new LeaderboardEntry
                    {
                        Rank = TryReadInt(row, "order", i + 1),
                        Nickname = TryReadString(row, "nickname"),
                        Score = TryReadLong(row, "score"),
                        UserId = TryReadString(row, "gamer"),
                    };
                    list.Add(entry);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendLeaderboard] 응답 파싱 실패: {e.Message}");
            }
            return list;
        }

        private static string TryReadString(JsonData row, string field)
        {
            if (row == null || !row.ContainsKey(field)) return string.Empty;
            var v = row[field];
            return v == null ? string.Empty : v.ToString();
        }

        private static int TryReadInt(JsonData row, string field, int fallback)
        {
            if (row == null || !row.ContainsKey(field)) return fallback;
            var v = row[field];
            if (v == null) return fallback;
            if (v.IsInt) return (int)v;
            if (v.IsLong) return (int)(long)v;
            if (v.IsDouble) return (int)(double)v;
            return int.TryParse(v.ToString(), out var parsed) ? parsed : fallback;
        }

        private static long TryReadLong(JsonData row, string field)
        {
            if (row == null || !row.ContainsKey(field)) return 0L;
            var v = row[field];
            if (v == null) return 0L;
            if (v.IsLong) return (long)v;
            if (v.IsInt) return (int)v;
            if (v.IsDouble) return (long)(double)v;
            return long.TryParse(v.ToString(), out var parsed) ? parsed : 0L;
        }
    }
}
