using System;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 SDK 상태코드를 프레임워크 중립 에러로 매핑한 값.
    /// </summary>
    public enum BackendError
    {
        None,
        Unknown,
        NotInitialized,
        NotAuthenticated,
        NetworkError,
        Timeout,
        InvalidRequest,
        ServerError,
        AlreadyExists,
        NotFound,
        PermissionDenied,
    }

    public enum BackendEnvironment
    {
        Dev,
        Staging,
        Production,
    }

    public enum CloudSaveConflictStrategy
    {
        LocalWins,
        RemoteWins,
        PreferNewest,
        Manual,
    }

    /// <summary>
    /// 리더보드 논리키. 실제 uuid 는 BackendConfig.GetLeaderboardUuid(key) 로 조회.
    /// </summary>
    public enum LeaderboardKey
    {
        HighScore,
        WeeklyScore,
        TotalKills,
    }

    /// <summary>
    /// 유연 테이블 논리키. 실제 테이블명은 BackendConfig.GetFlexibleTableName(key) 로 조회.
    /// </summary>
    public enum FlexibleTableKey
    {
        ShopCatalog,
        EventSchedule,
        RankingMeta,
    }

    [Serializable]
    public struct LeaderboardEntry
    {
        public int Rank;
        public string Nickname;
        public long Score;
        public string UserId;
    }

    /// <summary>
    /// Analytics 이벤트 카테고리. 뒤끝 GameLog 테이블명/태그로 매핑된다.
    /// </summary>
    public enum AnalyticsCategory
    {
        Progress,
        Economy,
        Combat,
        UI,
        Session,
        Error,
        Custom,
    }
}
