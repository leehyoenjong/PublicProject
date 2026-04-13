using System;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 인증 (게스트/커스텀/토큰 재로그인/닉네임).
    /// </summary>
    public interface IBackendAuth : IService
    {
        bool IsAuthenticated { get; }

        void SignInGuest(Action<bool, BackendError, string> callback);
        void SignInCustom(string id, string pw, Action<bool, BackendError, string> callback);
        void TryAutoSignIn(Action<bool, BackendError, string> callback);
        void SignOut();

        string GetNickname();
        void SetNickname(string nickname, Action<bool, BackendError, string> callback);
    }
}
