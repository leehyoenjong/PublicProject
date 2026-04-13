using System;
using UnityEngine;
using BackEnd;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 베이스 초기화 및 서버 시간 조회. 다른 Backend* 서비스와 결합하지 않는다.
    /// </summary>
    public class BackendService : IBackendService
    {
        private const string ACTION_INITIALIZE = "Initialize";
        private const string ACTION_GET_SERVER_TIME = "GetServerTime";

        private readonly BackendConfig _config;
        private readonly IEventBus _eventBus;
        private bool _isReady;

        public bool IsReady => _isReady;

        public BackendService(BackendConfig config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Initialize(Action<bool> onComplete)
        {
            string version = _config != null ? _config.AppVersion : "1.0.0";
            try
            {
                // 뒤끝 SDK 공식 초기화는 파라미터 없는 `Backend.Initialize()` 를 사용한다.
                // AppVersion 은 뒤끝 콘솔/Settings(dll) 에서 관리하며, 런타임 로그용으로만 참조.
                var bro = Backend.Initialize();
                _isReady = bro.IsSuccess();
                Debug.Log($"[BackendService] 초기화: ok={_isReady}, code={bro.GetStatusCode()}, version={version}");

                _eventBus?.Publish(new BackendInitializedEvent { Success = _isReady });

                if (!_isReady)
                {
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_INITIALIZE, BackendErrorMapper.Map(bro), bro.GetMessage());
                }
                else
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                }

                onComplete?.Invoke(_isReady);
            }
            catch (Exception e)
            {
                _isReady = false;
                Debug.LogError($"[BackendService] 초기화 예외: {e.Message}");
                _eventBus?.Publish(new BackendInitializedEvent { Success = false });
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_INITIALIZE, BackendError.NetworkError, e.Message);
                onComplete?.Invoke(false);
            }
        }

        public void GetServerTime(Action<bool, DateTime> callback)
        {
            if (!_isReady)
            {
                Debug.LogWarning("[BackendService] 서버 시간 조회 중단: 초기화 전");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_SERVER_TIME, BackendError.NotInitialized, "not ready");
                callback?.Invoke(false, DateTime.UtcNow);
                return;
            }

            try
            {
                var bro = Backend.Utils.GetServerTime();
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    Debug.LogWarning($"[BackendService] 서버 시간 실패: code={bro.GetStatusCode()}");
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_SERVER_TIME, err, bro.GetMessage());
                    callback?.Invoke(false, DateTime.UtcNow);
                    return;
                }

                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);

                var json = bro.GetReturnValuetoJSON();
                var raw = json != null && json["utcTime"] != null ? json["utcTime"].ToString() : null;
                if (!string.IsNullOrEmpty(raw) && DateTime.TryParse(raw, out DateTime serverTime))
                {
                    callback?.Invoke(true, serverTime);
                    return;
                }

                Debug.LogWarning("[BackendService] 서버 시간 파싱 실패");
                callback?.Invoke(false, DateTime.UtcNow);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendService] 서버 시간 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GET_SERVER_TIME, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, DateTime.UtcNow);
            }
        }
    }
}
