using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 네트워크 요청 데이터.
    /// </summary>
    public class NetworkRequest
    {
        public string RequestId { get; set; }
        public string Url { get; set; }
        public HttpMethod Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public RequestPriority Priority { get; set; }
        public float TimeoutSeconds { get; set; }
        public int MaxRetries { get; set; }
        public bool RequiresAuth { get; set; }

        public NetworkRequest()
        {
            RequestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            Method = HttpMethod.GET;
            Headers = new Dictionary<string, string>();
            Priority = RequestPriority.Normal;
            TimeoutSeconds = 30f;
            MaxRetries = 3;
            RequiresAuth = true;
        }
    }
}
