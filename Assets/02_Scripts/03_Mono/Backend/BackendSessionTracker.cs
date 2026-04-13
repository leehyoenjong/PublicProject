using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Analytics 세션 자동 추적. Start / Foreground / Background / Quit / AbnormalQuit 이벤트를 GameLog 로 기록.
    /// - 세션 id 는 Guid 로 1회 생성. 모든 이벤트 props 에 `sessionId` 포함.
    /// - 세션 지속시간: Start 시각(_sessionStartUtc) + Foreground 전환 시각(_lastForegroundUtc) 저장 후 diff 로 계산.
    ///   단일 props `durationSec` 로 통일 — Background=이번 Foreground 구간 시간, Quit/AbnormalQuit=세션 전체 시간.
    ///   action 값으로 해석하여 소비한다.
    /// - AbnormalQuit: <see cref="BackendCrashReporter"/> 가 `MarkAbnormal()` 호출 시 Quit 이벤트를 AbnormalQuit 으로 승격.
    /// - PII 금지: UserInDate/Nickname 등 미포함.
    /// </summary>
    public class BackendSessionTracker : MonoBehaviour
    {
        private const string ACTION_START = "Start";
        private const string ACTION_FOREGROUND = "Foreground";
        private const string ACTION_BACKGROUND = "Background";
        private const string ACTION_QUIT = "Quit";
        private const string ACTION_ABNORMAL_QUIT = "AbnormalQuit";
        private const string PROP_SESSION_ID = "sessionId";
        // 의미: Background=이번 Foreground 구간 시간, Quit/AbnormalQuit=세션 전체 시간. action 과 함께 해석.
        private const string PROP_DURATION = "durationSec";

        private IBackendAnalytics _analytics;
        private string _sessionId;
        private bool _isInForeground = true;
        private DateTime _sessionStartUtc;
        private DateTime _lastForegroundUtc;
        private bool _abnormalQuit;

        public string CurrentSessionId => _sessionId;

        private void Awake()
        {
            _sessionId = Guid.NewGuid().ToString("N");
            _sessionStartUtc = DateTime.UtcNow;
            _lastForegroundUtc = _sessionStartUtc;
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
            TransitionForegroundState(!pause);
        }

        private void OnApplicationQuit()
        {
            // best-effort. 플랫폼/프로세스 강제 종료 시 전송 보장되지 않음.
            double totalDuration = ClampDuration(DateTime.UtcNow - _sessionStartUtc);
            if (_abnormalQuit)
                LogSessionEvent(ACTION_ABNORMAL_QUIT, durationSec: totalDuration);
            else
                LogSessionEvent(ACTION_QUIT, durationSec: totalDuration);
        }

        /// <summary>
        /// 다음 Quit 를 AbnormalQuit 으로 승격. CrashReporter 가 호출하며, 여러 번 호출되어도 동작은 1회만 반영된다(idempotent).
        /// </summary>
        public void MarkAbnormal()
        {
            if (_abnormalQuit) return;
            _abnormalQuit = true;
            Debug.LogWarning($"[BackendSessionTracker] AbnormalQuit 표시 session={_sessionId}");
        }

        private void TransitionForegroundState(bool nextForeground)
        {
            if (nextForeground == _isInForeground) return;
            _isInForeground = nextForeground;

            if (nextForeground)
            {
                _lastForegroundUtc = DateTime.UtcNow;
                LogSessionEvent(ACTION_FOREGROUND);
            }
            else
            {
                double fgDuration = ClampDuration(DateTime.UtcNow - _lastForegroundUtc);
                LogSessionEvent(ACTION_BACKGROUND, durationSec: fgDuration);
            }
        }

        private void LogSessionEvent(string action, double? durationSec = null)
        {
            if (_analytics == null)
                _analytics = ResolveAnalytics();
            if (_analytics == null || !_analytics.IsEnabled) return;

            var props = new Dictionary<string, object>
            {
                { PROP_SESSION_ID, _sessionId },
            };
            if (durationSec.HasValue)
                props[PROP_DURATION] = durationSec.Value;

            _analytics.LogEvent(AnalyticsCategory.Session, action, props);
        }

        private static double ClampDuration(TimeSpan span)
        {
            if (span.TotalSeconds < 0)
            {
                Debug.LogWarning($"[BackendSessionTracker] 음수 duration 감지 ({span.TotalSeconds:F3}) — 시계 조정 가능성. 0 으로 clamp.");
                return 0.0;
            }
            return span.TotalSeconds;
        }

        private static IBackendAnalytics ResolveAnalytics()
        {
            return ServiceLocator.Has<IBackendAnalytics>() ? ServiceLocator.Get<IBackendAnalytics>() : null;
        }
    }
}
