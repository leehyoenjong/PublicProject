namespace PublicFramework
{
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH
    }

    public enum NetworkError
    {
        None,
        Timeout,
        ConnectionError,
        ServerError,
        AuthenticationFailed,
        NotFound,
        RateLimited,
        Cancelled,
        Unknown
    }

    public enum RequestPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}
