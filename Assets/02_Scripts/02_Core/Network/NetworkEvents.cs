namespace PublicFramework
{
    public struct NetworkRequestStartEvent
    {
        public string RequestId;
        public string Url;
        public HttpMethod Method;
    }

    public struct NetworkRequestCompleteEvent
    {
        public string RequestId;
        public int StatusCode;
        public float ElapsedTime;
    }

    public struct NetworkRequestFailEvent
    {
        public string RequestId;
        public NetworkError Error;
        public string ErrorMessage;
    }

    public struct NetworkRetryEvent
    {
        public string RequestId;
        public int AttemptNumber;
        public float DelaySeconds;
    }

    public struct NetworkConnectivityChangedEvent
    {
        public bool IsConnected;
    }

    public struct NetworkAuthRefreshEvent
    {
        public bool Success;
        public string RequestId;
    }
}
