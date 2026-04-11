using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 네트워크 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "PublicFramework/Network/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("기본 설정")]
        [SerializeField] private string _baseUrl = "https://api.example.com";
        [SerializeField] private float _defaultTimeout = 30f;
        [SerializeField] private int _maxConcurrentRequests = 4;

        [Header("재시도 정책")]
        [SerializeField] private int _maxRetries = 3;
        [SerializeField] private float _baseRetryDelay = 1f;
        [SerializeField] private float _maxRetryDelay = 30f;
        [SerializeField] private float _backoffMultiplier = 2f;

        public string BaseUrl => _baseUrl;
        public float DefaultTimeout => _defaultTimeout;
        public int MaxConcurrentRequests => _maxConcurrentRequests;
        public int MaxRetries => _maxRetries;
        public float BaseRetryDelay => _baseRetryDelay;
        public float MaxRetryDelay => _maxRetryDelay;
        public float BackoffMultiplier => _backoffMultiplier;
    }
}
