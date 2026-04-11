using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 개발용 더미 인증 토큰 제공자.
    /// </summary>
    public class DummyAuthTokenProvider : IAuthTokenProvider
    {
        public bool IsTokenValid => true;

        public string GetAccessToken()
        {
            return "dummy_access_token";
        }

        public void RefreshToken(Action<bool> onComplete)
        {
            Debug.Log("[DummyAuth] Token refreshed");
            onComplete?.Invoke(true);
        }
    }
}
