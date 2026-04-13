using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 Match/Realtime 래퍼.
    /// NOTE: 뒤끝 Match SDK 공식 문서(/sdk-docs/backend/match/) 접근 실패(404) 로 실제 시그니처가
    ///       확정되지 않은 상태이며, 프로젝트별 Match 서버 UUID/설정도 미정이다.
    ///       이에 현 Phase 10 구현은 **리플렉션 가드 + NotInitialized fallback** 로만 동작한다.
    ///       실제 호출 본체는 Match SDK 시그니처 확정 후 Phase 11+ 에서 이관.
    ///       (이벤트 구독 API 는 스텁 단계에서는 발행 경로가 없다.)
    /// </summary>
    public class BackendRealtime : IBackendRealtime
    {
        private const string ACTION_CONNECT = "RealtimeConnect";
        private const string ACTION_SEND = "RealtimeSend";

        private const string MATCH_TYPE_CANDIDATE = "BackEnd.Match, Backend";

        private readonly IEventBus _eventBus;
        private bool _typeChecked;
        private bool _typeAvailable;

        public bool IsConnected { get; private set; }

        public event Action<byte[]> OnMessageReceived;
        public event Action<BackendError, string> OnError;

        public BackendRealtime(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Connect(string roomToken, Action<bool, BackendError, string> callback)
        {
            if (string.IsNullOrEmpty(roomToken))
            {
                callback?.Invoke(false, BackendError.InvalidRequest, "roomToken empty");
                return;
            }

            if (!EnsureType())
            {
                Debug.LogWarning("[BackendRealtime] Match SDK 미감지 — Connect 건너뜀");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CONNECT, BackendError.NotInitialized, "match sdk missing");
                OnError?.Invoke(BackendError.NotInitialized, "match sdk missing");
                callback?.Invoke(false, BackendError.NotInitialized, "match sdk missing");
                return;
            }

            // Match SDK 실 시그니처 확정 시 리플렉션 호출로 교체. 현재는 성공 플래그만 시뮬레이션.
            Debug.LogWarning("[BackendRealtime] Connect: 실 호출 미구현 (Phase 11+ 이관). NotInitialized 반환.");
            BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CONNECT, BackendError.NotInitialized, "connect unimplemented");
            callback?.Invoke(false, BackendError.NotInitialized, "connect unimplemented");
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            Debug.Log("[BackendRealtime] Disconnect (스텁)");
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[BackendRealtime] Send 중단: 미연결");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SEND, BackendError.NotInitialized, "not connected");
                OnError?.Invoke(BackendError.NotInitialized, "not connected");
                return;
            }

            // 실 호출 미구현. 이벤트 구독자에게 샘플 에코는 하지 않는다(테스트 소음 방지).
            Debug.Log($"[BackendRealtime] Send (스텁): {(data != null ? data.Length : 0)}B");
        }

        private bool EnsureType()
        {
            if (_typeChecked) return _typeAvailable;
            _typeChecked = true;

            try
            {
                // BackEnd.Match 정적 클래스 감지. 시그니처 불명으로 멤버 호출은 보류.
                var matchType = Type.GetType(MATCH_TYPE_CANDIDATE, throwOnError: false);
                _typeAvailable = matchType != null;
                if (_typeAvailable)
                    Debug.Log("[BackendRealtime] BackEnd.Match 타입 감지 완료");
            }
            catch (Exception e)
            {
                _typeAvailable = false;
                Debug.LogError($"[BackendRealtime] 타입 감지 예외: {e.Message}");
            }

            return _typeAvailable;
        }

        internal void DispatchReceived(byte[] data)
        {
            if (data == null) return;
            OnMessageReceived?.Invoke(data);
            _eventBus?.Publish(new BackendRealtimeMessageEvent { Data = data });
        }

        internal void DispatchError(BackendError err, string msg)
        {
            OnError?.Invoke(err, msg);
        }
    }
}
