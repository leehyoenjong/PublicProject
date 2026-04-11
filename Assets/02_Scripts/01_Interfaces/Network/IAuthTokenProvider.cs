using System;

namespace PublicFramework
{
    /// <summary>
    /// 인증 토큰 제공 인터페이스.
    /// </summary>
    public interface IAuthTokenProvider
    {
        string GetAccessToken();
        void RefreshToken(Action<bool> onComplete);
        bool IsTokenValid { get; }
    }
}
