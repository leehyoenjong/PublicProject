using System;

namespace PublicFramework
{
    // 뒤끝 시스템 이벤트 (EventBus 로 발행). 7종.

    [Serializable]
    public struct BackendInitializedEvent
    {
        public bool Success;
    }

    /// <summary>
    /// 인증 상태 변화. SignedIn/SignedOut 을 통합한 단일 이벤트.
    /// </summary>
    [Serializable]
    public struct BackendAuthChangedEvent
    {
        public bool IsAuthenticated;
        public string UserInDate;
    }

    /// <summary>
    /// 리더보드 제출/조회가 발생했음을 알림. 구체 데이터는 콜백으로 전달.
    /// </summary>
    [Serializable]
    public struct BackendLeaderboardUpdatedEvent
    {
        public string Uuid;
    }

    [Serializable]
    public struct BackendMailFetchedEvent
    {
        public int Count;
    }

    [Serializable]
    public struct BackendCloudSaveSyncedEvent
    {
        public int Slot;
        public bool IsUpload;
        public bool Success;
    }

    /// <summary>
    /// 뒤끝 호출 실패 공통 이벤트. 각 서비스의 실패 경로에서 발행.
    /// </summary>
    [Serializable]
    public struct BackendCallFailedEvent
    {
        public string Action;
        public BackendError Error;
        public string Message;
    }

    /// <summary>
    /// 연결 상태 변경. NetworkError 최초 감지 또는 복구 시 1회 발행.
    /// </summary>
    [Serializable]
    public struct BackendConnectivityChangedEvent
    {
        public bool IsConnected;
    }

    [Serializable]
    public struct BackendAnalyticsLoggedEvent
    {
        public AnalyticsCategory Category;
        public string Action;
        public bool Success;
    }

    [Serializable]
    public struct BackendRealtimeMessageEvent
    {
        public byte[] Data;
    }

    [Serializable]
    public struct BackendCrashReportedEvent
    {
        public string ConditionPreview;
        public string StackHash;
    }

    [Serializable]
    public struct BackendConsentChangedEvent
    {
        public bool Required;
        public bool Analytics;
        public bool Marketing;
        public bool Functional;

        /// <summary>
        /// [Obsolete] 이전 버전 호환용 — <see cref="Analytics"/> 값을 그대로 반영.
        /// </summary>
        [Obsolete("Use Analytics instead")]
        public bool Accepted;
    }
}
