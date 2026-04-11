namespace PublicFramework
{
    /// <summary>
    /// 요청 옵션. 재시도 정책 설정.
    /// </summary>
    public class RequestOptions
    {
        public int MaxRetries { get; set; } = 3;
        public float BaseRetryDelay { get; set; } = 1f;
        public float MaxRetryDelay { get; set; } = 30f;
        public float BackoffMultiplier { get; set; } = 2f;
        public bool RetryOnTimeout { get; set; } = true;
        public bool RetryOnServerError { get; set; } = true;

        public float GetRetryDelay(int attempt)
        {
            float delay = BaseRetryDelay * UnityEngine.Mathf.Pow(BackoffMultiplier, attempt);
            return UnityEngine.Mathf.Min(delay, MaxRetryDelay);
        }
    }
}
