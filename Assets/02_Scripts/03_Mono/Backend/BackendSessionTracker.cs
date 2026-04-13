using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Analytics 세션 자동 추적. Start / Foreground / Background / Quit 이벤트를 GameLog 로 기록.
    /// - 세션 id 는 Guid 로 1회 생성되어 모든 이벤트의 props 에 `sessionId` 로 포함된다.
    /// - PII 금지: UserInDate/Nickname 등은 포함하지 않는다.
    /// - Focus 와 Pause 변화가 동시에 일어나는 플랫폼에서도 상태 diff 체크로 중복 발행을 방지한다.
    /// - Bootstrapper 가 AnalyticsEnabled && AnalyticsSessionAutoTrack 조건 하에 자동 생성/유지.
    /// </summary>
    public class BackendSessionTracker : MonoBehaviour
    {
        private const string CATEGORY_SESSION = "Session";
        private const string ACTION_START = "Start";
        private const string ACTION_FOREGROUND = "Foreground";
        private const string ACTION_BACKGROUND = "Background";
        private const string ACTION_QUIT = "Quit";
        private const string PROP_SESSION_ID = "sessionId";

        private IBackendAnalytics _analytics;
        private string _sessionId;
        private bool _isInForeground = true;

        public string CurrentSessionId => _sessionId;

        private void Awake()
        {
            _sessionId = Guid.NewGuid().ToString("N");
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _analytics = ResolveAnalytics();
            LogSessionEvent(ACTION_START);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            TransitionForegroundState(hasFocus);
        }

        private void OnApplicationPause(bool pause)
        {
            // pause=true → background, pause=false → foreground 복귀
            TransitionForegroundState(!pause);
        }

        private void OnApplicationQuit()
        {
            // Quit 이벤트는 best-effort — 플랫폼/프로세스 강제 종료 시 전송 보장되지 않을 수 있음.
            LogSessionEvent(ACTION_QUIT);
        }

        private void TransitionForegroundState(bool nextForeground)
        {
            if (nextForeground == _isInForeground) return;
            _isInForeground = nextForeground;
            LogSessionEvent(nextForeground ? ACTION_FOREGROUND : ACTION_BACKGROUND);
        }

        private void LogSessionEvent(string action)
        {
            if (_analytics == null)
                _analytics = ResolveAnalytics();
            if (_analytics == null || !_analytics.IsEnabled) return;

            var props = new Dictionary<string, object>
            {
                { PROP_SESSION_ID, _sessionId },
            };
            _analytics.LogEvent(AnalyticsCategory.Session, action, props);
        }

        private static IBackendAnalytics ResolveAnalytics()
        {
            return ServiceLocator.Has<IBackendAnalytics>() ? ServiceLocator.Get<IBackendAnalytics>() : null;
        }
    }
}
