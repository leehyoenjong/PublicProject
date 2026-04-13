namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 서비스 공통 이벤트 발행 헬퍼.
    /// 실패 이벤트 + NetworkError 기반 Connectivity 변화 감지를 일원화한다.
    /// </summary>
    internal static class BackendEventDispatcher
    {
        private static bool _lastConnected = true;

        public static void PublishFailed(IEventBus bus, string action, BackendError error, string message)
        {
            if (bus == null) return;

            bus.Publish(new BackendCallFailedEvent
            {
                Action = action,
                Error = error,
                Message = message ?? string.Empty,
            });

            if (error == BackendError.NetworkError && _lastConnected)
            {
                _lastConnected = false;
                bus.Publish(new BackendConnectivityChangedEvent { IsConnected = false });
            }
        }

        public static void NotifyOnlineIfRecovered(IEventBus bus)
        {
            if (bus == null) return;
            if (_lastConnected) return;

            _lastConnected = true;
            bus.Publish(new BackendConnectivityChangedEvent { IsConnected = true });
        }
    }
}
