using System;

namespace PublicFramework
{
    /// <summary>
    /// 리모트 푸시 제공 인터페이스.
    /// </summary>
    public interface IRemotePushProvider
    {
        void RegisterForRemotePush(Action<string> onTokenReceived, Action<string> onError);
        void UnregisterFromRemotePush();
        string GetPushToken();
    }
}
