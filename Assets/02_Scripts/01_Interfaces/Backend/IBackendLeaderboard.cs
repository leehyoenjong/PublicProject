using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 유저 리더보드 제출/조회. 논리키(enum) 를 사용하며,
    /// 실제 uuid 는 구현체가 BackendConfig 에서 조회한다.
    /// </summary>
    public interface IBackendLeaderboard : IService
    {
        void SubmitScore(LeaderboardKey key, long score, Action<bool, BackendError, string> callback);
        void GetTop(LeaderboardKey key, int limit, Action<bool, IReadOnlyList<LeaderboardEntry>, BackendError> callback);
        void GetMyRank(LeaderboardKey key, Action<bool, LeaderboardEntry, BackendError> callback);
        void GetAround(LeaderboardKey key, int range, Action<bool, IReadOnlyList<LeaderboardEntry>, BackendError> callback);
    }
}
