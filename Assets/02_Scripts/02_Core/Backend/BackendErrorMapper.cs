using BackEnd;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 BackendReturnObject 상태코드를 프레임워크 BackendError 로 매핑.
    /// 구현체 내부에서만 사용. 인터페이스 경계 밖으로 BRO 노출 금지.
    /// </summary>
    public static class BackendErrorMapper
    {
        // HTTP 상태코드 (문자열 — 뒤끝 SDK가 string으로 반환)
        private const string HTTP_OK = "200";
        private const string HTTP_CREATED = "201";
        private const string HTTP_BAD_REQUEST = "400";
        private const string HTTP_UNAUTHORIZED = "401";
        private const string HTTP_FORBIDDEN = "403";
        private const string HTTP_NOT_FOUND = "404";
        private const string HTTP_REQUEST_TIMEOUT = "408";
        private const string HTTP_CONFLICT = "409";
        private const string HTTP_PRECONDITION_FAILED = "412";
        private const string HTTP_UNPROCESSABLE = "422";
        private const string HTTP_INTERNAL_ERROR = "500";
        private const string HTTP_BAD_GATEWAY = "502";
        private const string HTTP_SERVICE_UNAVAILABLE = "503";
        private const string HTTP_GATEWAY_TIMEOUT = "504";

        public static BackendError Map(BackendReturnObject bro)
        {
            if (bro == null) return BackendError.Unknown;
            if (bro.IsSuccess()) return BackendError.None;
            return MapStatusCode(bro.GetStatusCode());
        }

        public static BackendError MapStatusCode(string statusCode)
        {
            if (string.IsNullOrEmpty(statusCode)) return BackendError.Unknown;

            switch (statusCode)
            {
                case HTTP_OK:
                case HTTP_CREATED:
                    return BackendError.None;
                case HTTP_BAD_REQUEST:
                case HTTP_PRECONDITION_FAILED:
                case HTTP_UNPROCESSABLE:
                    return BackendError.InvalidRequest;
                case HTTP_UNAUTHORIZED:
                    return BackendError.NotAuthenticated;
                case HTTP_FORBIDDEN:
                    return BackendError.PermissionDenied;
                case HTTP_NOT_FOUND:
                    return BackendError.NotFound;
                case HTTP_REQUEST_TIMEOUT:
                    return BackendError.Timeout;
                case HTTP_CONFLICT:
                    return BackendError.AlreadyExists;
                case HTTP_INTERNAL_ERROR:
                case HTTP_BAD_GATEWAY:
                case HTTP_SERVICE_UNAVAILABLE:
                case HTTP_GATEWAY_TIMEOUT:
                    return BackendError.ServerError;
                default:
                    return BackendError.Unknown;
            }
        }
    }
}
