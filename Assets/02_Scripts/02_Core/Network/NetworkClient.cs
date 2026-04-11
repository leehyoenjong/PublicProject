using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PublicFramework
{
    /// <summary>
    /// INetworkClient 구현체. UnityWebRequest + Coroutine 기반.
    /// 재시도(지수 백오프), 401 토큰 갱신, 네트워크 단절 감지, 요청 큐.
    /// </summary>
    public class NetworkClient : INetworkClient
    {
        private readonly IAuthTokenProvider _authProvider;
        private readonly INetworkSerializer _serializer;
        private readonly IEventBus _eventBus;
        private readonly NetworkConfig _config;
        private readonly RequestQueue _requestQueue;
        private readonly RequestOptions _retryOptions;
        private readonly MonoBehaviour _coroutineRunner;

        private readonly Dictionary<string, Action<NetworkResponse>> _callbacks = new Dictionary<string, Action<NetworkResponse>>();
        private bool _isConnected = true;

        public bool IsConnected => _isConnected;
        public int PendingRequestCount => _requestQueue.PendingCount + _requestQueue.ActiveCount;

        public NetworkClient(IAuthTokenProvider authProvider, INetworkSerializer serializer,
            IEventBus eventBus, NetworkConfig config, MonoBehaviour coroutineRunner)
        {
            _authProvider = authProvider;
            _serializer = serializer;
            _eventBus = eventBus;
            _config = config;
            _coroutineRunner = coroutineRunner;
            _requestQueue = new RequestQueue(config != null ? config.MaxConcurrentRequests : 4);
            _retryOptions = new RequestOptions();

            Debug.Log("[NetworkClient] Init started");
        }

        public void SendRequest(NetworkRequest request, Action<NetworkResponse> onComplete)
        {
            if (request == null)
            {
                Debug.LogError("[NetworkClient] Request is null");
                return;
            }

            _callbacks[request.RequestId] = onComplete;

            _requestQueue.Enqueue(request, req => _coroutineRunner.StartCoroutine(ExecuteRequest(req, 0)));
        }

        public void CancelRequest(string requestId)
        {
            _requestQueue.Cancel(requestId);
            _callbacks.Remove(requestId);

            Debug.Log($"[NetworkClient] Cancelled: {requestId}");
        }

        public void CancelAll()
        {
            _requestQueue.CancelAll();
            _callbacks.Clear();

            Debug.Log("[NetworkClient] All requests cancelled");
        }

        private IEnumerator ExecuteRequest(NetworkRequest request, int attempt)
        {
            _eventBus?.Publish(new NetworkRequestStartEvent
            {
                RequestId = request.RequestId,
                Url = request.Url,
                Method = request.Method
            });

            float startTime = Time.realtimeSinceStartup;

            using UnityWebRequest webRequest = CreateWebRequest(request);

            if (request.RequiresAuth && _authProvider != null)
            {
                string token = _authProvider.GetAccessToken();
                webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            webRequest.timeout = Mathf.RoundToInt(request.TimeoutSeconds);

            yield return webRequest.SendWebRequest();

            float elapsed = Time.realtimeSinceStartup - startTime;
            NetworkResponse response = ParseResponse(request.RequestId, webRequest, elapsed);

            // 네트워크 단절 감지
            bool wasConnected = _isConnected;
            _isConnected = response.Error != NetworkError.ConnectionError;

            if (wasConnected != _isConnected)
            {
                _eventBus?.Publish(new NetworkConnectivityChangedEvent { IsConnected = _isConnected });
            }

            // 401 → 토큰 갱신 후 재시도
            if (response.StatusCode == 401 && _authProvider != null && attempt == 0)
            {
                bool refreshDone = false;
                bool refreshed = false;
                _authProvider.RefreshToken(success =>
                {
                    refreshed = success;
                    refreshDone = true;
                });

                yield return new WaitUntil(() => refreshDone);

                _eventBus?.Publish(new NetworkAuthRefreshEvent
                {
                    Success = refreshed,
                    RequestId = request.RequestId
                });

                if (refreshed)
                {
                    Debug.Log($"[NetworkClient] Token refreshed, retrying: {request.RequestId}");
                    yield return ExecuteRequest(request, attempt + 1);
                    yield break;
                }
            }

            // 재시도 가능한 에러
            if (!response.IsSuccess && attempt < request.MaxRetries && ShouldRetry(response))
            {
                float delay = _retryOptions.GetRetryDelay(attempt);

                _eventBus?.Publish(new NetworkRetryEvent
                {
                    RequestId = request.RequestId,
                    AttemptNumber = attempt + 1,
                    DelaySeconds = delay
                });

                Debug.Log($"[NetworkClient] Retry {attempt + 1}/{request.MaxRetries}: {request.RequestId} (delay: {delay:F1}s)");

                yield return new WaitForSeconds(delay);
                yield return ExecuteRequest(request, attempt + 1);
                yield break;
            }

            // 완료
            _requestQueue.OnRequestComplete();

            if (response.IsSuccess)
            {
                _eventBus?.Publish(new NetworkRequestCompleteEvent
                {
                    RequestId = request.RequestId,
                    StatusCode = response.StatusCode,
                    ElapsedTime = elapsed
                });
            }
            else
            {
                _eventBus?.Publish(new NetworkRequestFailEvent
                {
                    RequestId = request.RequestId,
                    Error = response.Error,
                    ErrorMessage = response.ErrorMessage
                });
            }

            if (_callbacks.TryGetValue(request.RequestId, out Action<NetworkResponse> callback))
            {
                _callbacks.Remove(request.RequestId);
                callback?.Invoke(response);
            }

            Debug.Log($"[NetworkClient] {(response.IsSuccess ? "OK" : "FAIL")}: {request.RequestId} ({response.StatusCode}, {elapsed:F2}s)");
        }

        private UnityWebRequest CreateWebRequest(NetworkRequest request)
        {
            UnityWebRequest webRequest = request.Method switch
            {
                HttpMethod.POST => new UnityWebRequest(request.Url, "POST"),
                HttpMethod.PUT => new UnityWebRequest(request.Url, "PUT"),
                HttpMethod.DELETE => new UnityWebRequest(request.Url, "DELETE"),
                HttpMethod.PATCH => new UnityWebRequest(request.Url, "PATCH"),
                _ => UnityWebRequest.Get(request.Url)
            };

            if (!string.IsNullOrEmpty(request.Body) && request.Method != HttpMethod.GET)
            {
                byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(request.Body);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            webRequest.downloadHandler = new DownloadHandlerBuffer();

            if (request.Headers != null)
            {
                foreach (var kvp in request.Headers)
                {
                    webRequest.SetRequestHeader(kvp.Key, kvp.Value);
                }
            }

            return webRequest;
        }

        private NetworkResponse ParseResponse(string requestId, UnityWebRequest webRequest, float elapsed)
        {
            var response = new NetworkResponse
            {
                RequestId = requestId,
                StatusCode = (int)webRequest.responseCode,
                Body = webRequest.downloadHandler?.text,
                ElapsedTime = elapsed
            };

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                response.Error = NetworkError.None;
            }
            else if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                response.Error = NetworkError.ConnectionError;
                response.ErrorMessage = webRequest.error;
            }
            else if (webRequest.responseCode == 401)
            {
                response.Error = NetworkError.AuthenticationFailed;
                response.ErrorMessage = "Authentication failed";
            }
            else if (webRequest.responseCode == 404)
            {
                response.Error = NetworkError.NotFound;
                response.ErrorMessage = "Not found";
            }
            else if (webRequest.responseCode == 429)
            {
                response.Error = NetworkError.RateLimited;
                response.ErrorMessage = "Rate limited";
            }
            else if (webRequest.responseCode >= 500)
            {
                response.Error = NetworkError.ServerError;
                response.ErrorMessage = webRequest.error;
            }
            else
            {
                response.Error = NetworkError.Unknown;
                response.ErrorMessage = webRequest.error;
            }

            return response;
        }

        private bool ShouldRetry(NetworkResponse response)
        {
            return response.Error == NetworkError.Timeout
                || response.Error == NetworkError.ConnectionError
                || response.Error == NetworkError.ServerError
                || response.Error == NetworkError.RateLimited;
        }
    }
}
