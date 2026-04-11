using System;

namespace PublicFramework
{
    /// <summary>
    /// 네트워크 통신 서비스 인터페이스.
    /// </summary>
    public interface INetworkClient : IService
    {
        void SendRequest(NetworkRequest request, Action<NetworkResponse> onComplete);
        void CancelRequest(string requestId);
        void CancelAll();
        bool IsConnected { get; }
        int PendingRequestCount { get; }
    }
}
