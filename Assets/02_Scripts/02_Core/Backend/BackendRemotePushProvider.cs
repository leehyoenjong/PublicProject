using System;
using System.Reflection;
using UnityEngine;
using BackEnd;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 원격 푸시 토큰 등록 Provider.
    /// NOTE: 뒤끝 Push SDK 공식 문서 접근 실패로 `Backend.PushNotification` 정적 타입 FQN 이 확정되지 않았다.
    ///       이에 `RegisterForRemotePush`/`UnregisterFromRemotePush` 본체는 **리플렉션 타입 감지 + Invoke** 로 전환되었으며,
    ///       타입 감지 실패 시 `NotInitialized` + CallFailed 콜백으로 안전 폴백한다.
    ///       실제 FQN 확정 시 <see cref="PUSH_IOS_TYPE_CANDIDATE"/>/<see cref="PUSH_AOS_TYPE_CANDIDATE"/> 후보 상수만 교체해 주면 된다.
    ///
    /// 플랫폼 토큰(APNs/FCM) 획득은 프로젝트 플러그인 담당 — 획득 후 <see cref="SetToken"/> 로 주입.
    /// 기존 <see cref="IRemotePushProvider"/> 시그니처는 변경 없음.
    /// </summary>
    public class BackendRemotePushProvider : IRemotePushProvider
    {
        private const string ACTION_REGISTER = "PushRegister";
        private const string ACTION_UNREGISTER = "PushUnregister";

        // 후보 FQN (Phase 11+ 문서 확인 시 교정). Push SDK 공식 문서 미공개 상태라 추정치.
        private const string PUSH_IOS_TYPE_CANDIDATE = "BackEnd.iOS.PushNotification, Backend";
        private const string PUSH_AOS_TYPE_CANDIDATE = "BackEnd.AOS.PushNotification, Backend";

        private const string METHOD_INSERT = "InsertPushToken";
        private const string METHOD_DELETE = "DeletePushToken";

        private readonly IEventBus _eventBus;
        private string _cachedToken = string.Empty;

        public BackendRemotePushProvider(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 플랫폼 푸시 토큰을 외부(프로젝트 플러그인)에서 주입.
        /// </summary>
        public void SetToken(string token)
        {
            _cachedToken = token ?? string.Empty;
            Debug.Log($"[BackendPush] 토큰 주입 완료 (len={_cachedToken.Length})");
        }

        public void RegisterForRemotePush(Action<string> onTokenReceived, Action<string> onError)
        {
            if (string.IsNullOrEmpty(_cachedToken))
            {
                Debug.LogWarning("[BackendPush] 등록 중단: 토큰이 주입되지 않았음");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REGISTER, BackendError.InvalidRequest, "no token cached");
                onError?.Invoke("no token cached — call SetToken first");
                return;
            }

            var platformType = ResolvePlatformType();
            if (platformType == null)
            {
                Debug.LogWarning("[BackendPush] 등록 중단: 플랫폼/SDK Push 타입 미감지");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REGISTER, BackendError.NotInitialized, "push type not found");
                onError?.Invoke("push type not found");
                return;
            }

            var bro = InvokeStaticStringMethod(platformType, METHOD_INSERT, _cachedToken);
            if (bro == null)
            {
                Debug.LogWarning($"[BackendPush] 등록 중단: {platformType.FullName}.{METHOD_INSERT}(string) 메서드 미발견");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REGISTER, BackendError.NotInitialized, "insert method missing");
                onError?.Invoke("insert method missing");
                return;
            }

            var ok = bro.IsSuccess();
            Debug.Log($"[BackendPush] 토큰 등록: ok={ok}, code={bro.GetStatusCode()}");
            if (ok)
            {
                BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                onTokenReceived?.Invoke(_cachedToken);
            }
            else
            {
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_REGISTER, BackendErrorMapper.Map(bro), bro.GetMessage());
                onError?.Invoke(bro.GetMessage());
            }
        }

        public void UnregisterFromRemotePush()
        {
            var platformType = ResolvePlatformType();
            if (platformType == null)
            {
                Debug.LogWarning("[BackendPush] 해제 중단: 플랫폼/SDK Push 타입 미감지");
                return;
            }

            var bro = InvokeStaticNoArgMethod(platformType, METHOD_DELETE);
            if (bro == null)
            {
                Debug.LogWarning($"[BackendPush] 해제 중단: {platformType.FullName}.{METHOD_DELETE}() 메서드 미발견");
                return;
            }

            Debug.Log($"[BackendPush] 토큰 해제: ok={bro.IsSuccess()}, code={bro.GetStatusCode()}");
            if (!bro.IsSuccess())
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_UNREGISTER, BackendErrorMapper.Map(bro), bro.GetMessage());
        }

        public string GetPushToken() => _cachedToken;

        private static Type ResolvePlatformType()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return Type.GetType(PUSH_IOS_TYPE_CANDIDATE, throwOnError: false);
                case RuntimePlatform.Android:
                    return Type.GetType(PUSH_AOS_TYPE_CANDIDATE, throwOnError: false);
                default:
                    return null;
            }
        }

        private static BackendReturnObject InvokeStaticStringMethod(Type type, string methodName, string arg)
        {
            try
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (method == null) return null;
                return method.Invoke(null, new object[] { arg }) as BackendReturnObject;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendPush] {type.FullName}.{methodName}(string) 호출 예외: {e.Message}");
                return null;
            }
        }

        private static BackendReturnObject InvokeStaticNoArgMethod(Type type, string methodName)
        {
            try
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method == null) return null;
                return method.Invoke(null, null) as BackendReturnObject;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendPush] {type.FullName}.{methodName}() 호출 예외: {e.Message}");
                return null;
            }
        }
    }
}
