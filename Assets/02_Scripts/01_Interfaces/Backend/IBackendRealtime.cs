using System;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 Match/Realtime 래퍼 인터페이스.
    /// 구현체는 뒤끝 Match SDK(`Backend.Match.*`) 에 의존하며, SDK 미 import / 시그니처 불일치 시
    /// 리플렉션 가드로 `NotInitialized` fallback 한다.
    /// </summary>
    public interface IBackendRealtime : IService
    {
        bool IsConnected { get; }

        void Connect(string roomToken, Action<bool, BackendError, string> callback);
        void Disconnect();
        void Send(byte[] data);

        event Action<byte[]> OnMessageReceived;
        event Action<BackendError, string> OnError;
    }
}
