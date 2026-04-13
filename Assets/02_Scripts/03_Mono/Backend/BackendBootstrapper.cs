using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 네트워크 시스템 진입점.
    /// Startup chain:
    ///   1) SendQueueMgr 보장
    ///   2) BackendService.Initialize
    ///   3) AutoGuestLogin (TryAutoSignIn → 실패 시 SignInGuest)
    ///   4) AutoCloudSaveOnLogin (DownloadSlot 0)
    ///   5) SessionTracker (AnalyticsEnabled && AnalyticsSessionAutoTrack 조건부)
    ///   6) CrashReporter (CrashReporterEnabled 조건부)
    /// 인스펙터에 BackendConfig 를 주입한다. 내부에서 모든 서비스 인스턴스를 조립하고 ServiceLocator 에 등록.
    /// </summary>
    public class BackendBootstrapper : MonoBehaviour
    {
        private const int DEFAULT_CLOUD_SAVE_SLOT = 0;

        [SerializeField] private BackendConfig _config;

        private IBackendService _service;
        private IBackendAuth _auth;
        private IBackendLeaderboard _leaderboard;
        private IBackendDatabase _database;
        private IBackendMail _mail;
        private ICloudSaveSync _cloudSave;
        private IBackendAnalytics _analytics;
        private IBackendRealtime _realtime;
        private BackendRemotePushProvider _pushProvider;
        private BackendSessionTracker _sessionTracker;

        /// <summary>플랫폼 토큰 주입(SetToken) 및 Register 호출에 접근하기 위한 프로젝트측 참조.</summary>
        public BackendRemotePushProvider PushProvider => _pushProvider;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[BackendBootstrapper] BackendConfig 미주입 — 초기화 중단");
                return;
            }

            EnsureSendQueue();
            EnsureMainThreadDispatcher();

            IEventBus eventBus = ResolveEventBus();
            ISaveSystem saveSystem = ResolveSaveSystem();

            AssembleServices(eventBus, saveSystem);
            RegisterServices();
            EnsureSessionTracker();
            EnsureCrashReporter();
            RunStartupChain();
        }

        private void EnsureSendQueue()
        {
            if (!_config.SendQueueEnabled)
            {
                Debug.Log("[BackendBootstrapper] SendQueue 비활성 설정 — 생성 생략");
                return;
            }

            // Unity 6 신규 API. 구 FindObjectOfType<T> 는 obsolete.
            var existing = FindFirstObjectByType<SendQueueMgr>();
            if (existing != null)
            {
                DontDestroyOnLoad(existing.gameObject);
                Debug.Log("[BackendBootstrapper] 기존 SendQueueMgr 재사용");
                return;
            }

            var go = new GameObject("SendQueueMgr");
            go.AddComponent<SendQueueMgr>();
            DontDestroyOnLoad(go);
            Debug.Log("[BackendBootstrapper] SendQueueMgr 생성 완료");
        }

        private void EnsureMainThreadDispatcher()
        {
            if (BackendMainThreadDispatcher.Instance != null)
                return;

            var go = new GameObject("BackendMainThreadDispatcher");
            go.AddComponent<BackendMainThreadDispatcher>();
            // BackendMainThreadDispatcher.Awake 에서 DontDestroyOnLoad 처리.
            Debug.Log("[BackendBootstrapper] BackendMainThreadDispatcher 생성 완료");
        }

        private IEventBus ResolveEventBus()
        {
            if (ServiceLocator.Has<IEventBus>())
                return ServiceLocator.Get<IEventBus>();

            Debug.LogWarning("[BackendBootstrapper] IEventBus 미등록 — 기본 EventBus 생성");
            var bus = new EventBus();
            ServiceLocator.Register<IEventBus>(bus);
            return bus;
        }

        private ISaveSystem ResolveSaveSystem()
        {
            if (ServiceLocator.Has<ISaveSystem>())
                return ServiceLocator.Get<ISaveSystem>();

            Debug.LogWarning("[BackendBootstrapper] ISaveSystem 미등록 — 클라우드 세이브 기능 비활성");
            return null;
        }

        private void AssembleServices(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _service = new BackendService(_config, eventBus);
            _auth = new BackendAuth(eventBus);
            _leaderboard = new BackendLeaderboard(_config, eventBus);
            // BackendDatabase: BACKND.Database Client 는 최초 QueryFlexibleTable 호출 시 리플렉션 기반 지연 초기화됨.
            _database = new BackendDatabase(_config, eventBus);
            _mail = new BackendMailProvider(eventBus);
            _cloudSave = new CloudSaveSync(saveSystem, eventBus);
            // Analytics 는 opt-in. BackendConfig.AnalyticsEnabled 기본값 AND 사용자 동의(ConsentStore.Analytics) 두 조건 모두 만족 시 활성.
            bool analyticsOn = _config.AnalyticsEnabled && ConsentStore.GetConsent(ConsentCategory.Analytics);
            _analytics = new BackendAnalytics(eventBus) { IsEnabled = analyticsOn };
            _realtime = new BackendRealtime(eventBus);
            _pushProvider = new BackendRemotePushProvider(eventBus);
        }

        private void RegisterServices()
        {
            ServiceLocator.Register(_service);
            ServiceLocator.Register(_auth);
            ServiceLocator.Register(_leaderboard);
            ServiceLocator.Register(_database);
            ServiceLocator.Register(_mail);
            ServiceLocator.Register(_cloudSave);
            ServiceLocator.Register(_analytics);
            ServiceLocator.Register(_realtime);
            // IRemotePushProvider 는 기존 Notification 모듈 인터페이스이며 IService 미상속이라 ServiceLocator 에 넣지 않는다.
            // 프로젝트 코드는 BackendBootstrapper.PushProvider 경유로 접근한다.
            Debug.Log("[BackendBootstrapper] 서비스 8종 ServiceLocator 등록 완료 (+ PushProvider public 참조)");
        }

        private void EnsureSessionTracker()
        {
            if (!_config.AnalyticsEnabled || !_config.AnalyticsSessionAutoTrack)
                return;

            _sessionTracker = FindFirstObjectByType<BackendSessionTracker>();
            if (_sessionTracker != null)
            {
                Debug.Log("[BackendBootstrapper] 기존 BackendSessionTracker 재사용");
                return;
            }

            var go = new GameObject("BackendSessionTracker");
            _sessionTracker = go.AddComponent<BackendSessionTracker>();
            Debug.Log("[BackendBootstrapper] BackendSessionTracker 생성 완료");
        }

        private void EnsureCrashReporter()
        {
            if (!_config.CrashReporterEnabled)
                return;

            var existing = FindFirstObjectByType<BackendCrashReporter>();
            if (existing != null)
            {
                Debug.Log("[BackendBootstrapper] 기존 BackendCrashReporter 재사용");
                return;
            }

            IEventBus bus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            var go = new GameObject("BackendCrashReporter");
            var reporter = go.AddComponent<BackendCrashReporter>();
            reporter.Configure(_config, _analytics, bus, _sessionTracker);
            Debug.Log("[BackendBootstrapper] BackendCrashReporter 생성 완료");
        }

        private void RunStartupChain()
        {
            // A2: Initialize 실패는 네트워크/서버 이상으로 간주한다.
            //     본 프레임워크는 자동 재시도를 수행하지 않는다 — 프로젝트 레이어에서
            //     사용자에게 "다시 시도" UI 를 제공하고 SignInGuest/TryAutoSignIn 을 다시 호출할 것.
            _service.Initialize(initOk =>
            {
                if (!initOk)
                {
                    Debug.LogWarning("[BackendBootstrapper] 초기화 실패 — 후속 체인 중단 (프로젝트에서 수동 재시도 UI 구성 필요)");
                    return;
                }

                if (!_config.AutoGuestLogin)
                {
                    Debug.Log("[BackendBootstrapper] AutoGuestLogin 비활성 — 인증 체인 생략");
                    return;
                }

                _auth.TryAutoSignIn((autoOk, _, _) =>
                {
                    if (autoOk)
                    {
                        OnAuthenticated();
                        return;
                    }

                    Debug.Log("[BackendBootstrapper] 자동 로그인 실패 — 게스트 로그인 전환");
                    _auth.SignInGuest((guestOk, _, _) =>
                    {
                        if (guestOk) OnAuthenticated();
                    });
                });
            });
        }

        private void OnAuthenticated()
        {
            // DatabaseUuid 가 설정된 경우 BACKND.Database Client 를 사전에 비동기 초기화(fire-and-forget).
            if (!string.IsNullOrEmpty(_config.DatabaseUuid) && _database is BackendDatabase db)
            {
                _ = db.EnsureClientAsync();
            }

            if (!_config.AutoCloudSaveOnLogin)
                return;

            Debug.Log($"[BackendBootstrapper] 자동 클라우드 세이브 다운로드 시작: slot={DEFAULT_CLOUD_SAVE_SLOT}");
            _cloudSave.DownloadSlot(DEFAULT_CLOUD_SAVE_SLOT, null);
        }

        private void OnDestroy()
        {
            // BACKND.Database Client 리소스 정리.
            (_database as BackendDatabase)?.Dispose();
        }
    }
}
