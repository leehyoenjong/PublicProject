using System;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 베이스 초기화 및 서버 시간 조회.
    /// </summary>
    public interface IBackendService : IService
    {
        void Initialize(Action<bool> onComplete);
        bool IsReady { get; }
        void GetServerTime(Action<bool, DateTime> callback);
    }
}
