using System;
using UnityEngine;
using BackEnd;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 인증 (게스트/커스텀/토큰 자동/로그아웃/닉네임).
    /// </summary>
    public class BackendAuth : IBackendAuth
    {
        private const string ACTION_GUEST = "SignInGuest";
        private const string ACTION_CUSTOM = "SignInCustom";
        private const string ACTION_AUTO = "TryAutoSignIn";
        private const string ACTION_SET_NICK = "SetNickname";

        private readonly IEventBus _eventBus;

        public bool IsAuthenticated { get; private set; }

        public BackendAuth(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void SignInGuest(Action<bool, BackendError, string> callback)
        {
            try
            {
                var bro = Backend.BMember.GuestLogin();
                IsAuthenticated = bro.IsSuccess();
                Debug.Log($"[BackendAuth] 게스트 로그인: ok={IsAuthenticated}, code={bro.GetStatusCode()}");

                var err = BackendErrorMapper.Map(bro);
                if (IsAuthenticated)
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                    _eventBus?.Publish(new BackendAuthChangedEvent { IsAuthenticated = true, UserInDate = GetUserInDate() });
                }
                else
                {
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GUEST, err, bro.GetMessage());
                }

                callback?.Invoke(IsAuthenticated, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                IsAuthenticated = false;
                Debug.LogError($"[BackendAuth] 게스트 로그인 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_GUEST, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void SignInCustom(string id, string pw, Action<bool, BackendError, string> callback)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                Debug.LogWarning("[BackendAuth] 커스텀 로그인 중단: id/pw 비어있음");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CUSTOM, BackendError.InvalidRequest, "id/pw empty");
                callback?.Invoke(false, BackendError.InvalidRequest, "id/pw empty");
                return;
            }

            try
            {
                var bro = Backend.BMember.CustomLogin(id, pw);
                IsAuthenticated = bro.IsSuccess();
                Debug.Log($"[BackendAuth] 커스텀 로그인: ok={IsAuthenticated}, code={bro.GetStatusCode()}");

                var err = BackendErrorMapper.Map(bro);
                if (IsAuthenticated)
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                    _eventBus?.Publish(new BackendAuthChangedEvent { IsAuthenticated = true, UserInDate = GetUserInDate() });
                }
                else
                {
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CUSTOM, err, bro.GetMessage());
                }

                callback?.Invoke(IsAuthenticated, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                IsAuthenticated = false;
                Debug.LogError($"[BackendAuth] 커스텀 로그인 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_CUSTOM, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void TryAutoSignIn(Action<bool, BackendError, string> callback)
        {
            // P2: 토큰 보유 선체크 — 무의미한 네트워크 요청 차단.
            // 뒤끝 SDK: `Backend.BMember.IsAccessTokenAlive()` 는 BRO 반환 메서드 (bool 아님).
            // 성공 여부는 IsSuccess() 로 확인한다.
            try
            {
                var tokenBro = Backend.BMember.IsAccessTokenAlive();
                if (tokenBro == null || !tokenBro.IsSuccess())
                {
                    Debug.LogWarning("[BackendAuth] 자동 로그인 중단: access token 없음/만료");
                    callback?.Invoke(false, BackendError.NotAuthenticated, "no access token");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendAuth] 토큰 선체크 예외 (무시하고 재로그인 시도): {e.Message}");
            }

            try
            {
                var bro = Backend.BMember.LoginWithTheBackendToken();
                IsAuthenticated = bro.IsSuccess();
                Debug.Log($"[BackendAuth] 토큰 자동 로그인: ok={IsAuthenticated}, code={bro.GetStatusCode()}");

                var err = BackendErrorMapper.Map(bro);
                if (IsAuthenticated)
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                    _eventBus?.Publish(new BackendAuthChangedEvent { IsAuthenticated = true, UserInDate = GetUserInDate() });
                }
                else
                {
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_AUTO, err, bro.GetMessage());
                }

                callback?.Invoke(IsAuthenticated, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                IsAuthenticated = false;
                Debug.LogError($"[BackendAuth] 자동 로그인 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_AUTO, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        public void SignOut()
        {
            try
            {
                Backend.BMember.Logout();
                IsAuthenticated = false;
                Debug.Log("[BackendAuth] 로그아웃 완료");
                _eventBus?.Publish(new BackendAuthChangedEvent { IsAuthenticated = false, UserInDate = string.Empty });
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendAuth] 로그아웃 예외: {e.Message}");
            }
        }

        public string GetNickname()
        {
            try
            {
                return Backend.UserNickName ?? string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendAuth] 닉네임 조회 실패: {e.Message}");
                return string.Empty;
            }
        }

        public void SetNickname(string nickname, Action<bool, BackendError, string> callback)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogWarning("[BackendAuth] 닉네임 변경 중단: 값 비어있음");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SET_NICK, BackendError.InvalidRequest, "nickname empty");
                callback?.Invoke(false, BackendError.InvalidRequest, "nickname empty");
                return;
            }

            try
            {
                var bro = Backend.BMember.UpdateNickname(nickname);
                var ok = bro.IsSuccess();
                Debug.Log($"[BackendAuth] 닉네임 변경: ok={ok}, code={bro.GetStatusCode()}");

                var err = BackendErrorMapper.Map(bro);
                if (ok)
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                else
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SET_NICK, err, bro.GetMessage());

                callback?.Invoke(ok, err, bro.GetMessage());
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendAuth] 닉네임 변경 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_SET_NICK, BackendError.NetworkError, e.Message);
                callback?.Invoke(false, BackendError.NetworkError, e.Message);
            }
        }

        private static string GetUserInDate()
        {
            try
            {
                return Backend.UserInDate ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
