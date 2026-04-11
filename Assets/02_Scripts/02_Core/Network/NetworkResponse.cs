namespace PublicFramework
{
    /// <summary>
    /// 네트워크 응답 데이터.
    /// </summary>
    public class NetworkResponse
    {
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
        public string Body { get; set; }
        public NetworkError Error { get; set; }
        public string ErrorMessage { get; set; }
        public float ElapsedTime { get; set; }

        public bool IsSuccess => Error == NetworkError.None && StatusCode >= 200 && StatusCode < 300;
    }
}
