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
            Debug.Log("[알림] 리모트 푸시 등록됨.");
            onTokenReceived?.Invoke(DUMMY_TOKEN);
        }

        public void UnregisterFromRemotePush()
        {
            Debug.Log("[알림] 리모트 푸시 등록 해제됨.");
        }

        public string GetPushToken()
        {
            return DUMMY_TOKEN;
        }
    }
}
