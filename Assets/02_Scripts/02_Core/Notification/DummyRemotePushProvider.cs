using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 개발용 더미 리모트 푸시 제공자.
    /// </summary>
    public class DummyRemotePushProvider : IRemotePushProvider
    {
        private const string DUMMY_TOKEN = "dummy_push_token_12345";

        public void RegisterForRemotePush(Action<string> onTokenReceived, Action<string> onError)
        {
            Debug.Log("[DummyPush] Registered for remote push");
            onTokenReceived?.Invoke(DUMMY_TOKEN);
        }

        public void UnregisterFromRemotePush()
        {
            Debug.Log("[DummyPush] Unregistered from remote push");
        }

        public string GetPushToken()
        {
            return DUMMY_TOKEN;
        }
    }
}
