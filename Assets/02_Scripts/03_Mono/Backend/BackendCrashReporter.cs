using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 비핸들 예외/에러 로그를 Analytics 로 리포팅.
    /// - `Application.logMessageReceivedThreaded` 구독 → 백그라운드 스레드에서도 수신.
    /// - 실제 이벤트 발행은 <see cref="BackendMainThreadDispatcher"/> 경유 메인 스레드에서.
    /// - PII 금지: condition 은 200자 preview, stack 은 SHA1 해시(16자) 만 송출.
    /// - 5분 중복 방지 쓰로틀: 동일 stackHash 는 TTL 내 1회만.
    /// - Debug 빌드 + `_config.CrashIncludeFullStackInDebugOnly` 일 때만 전체 스택 추가.
    /// </summary>
    public class BackendCrashReporter : MonoBehaviour
    {
        private const string CATEGORY = "Error";
        private const string ACTION = "Crash";
        private const string PROP_CONDITION = "conditionPreview";
        private const string PROP_STACK = "stackHash";
        private const string PROP_SESSION = "sessionId";
        private const string PROP_FULL_STACK = "fullStack";
        private const int CONDITION_MAX_LEN = 200;
        private const int STACK_HASH_LEN = 16;
        private const double DEDUP_TTL_SEC = 300.0; // 5분

        private BackendConfig _config;
        private IBackendAnalytics _analytics;
        private IEventBus _eventBus;
        private BackendSessionTracker _sessionTracker;

        private readonly Dictionary<string, DateTime> _lastSent = new();
        private readonly object _lock = new();

        public void Configure(
            BackendConfig config,
            IBackendAnalytics analytics,
            IEventBus eventBus,
            BackendSessionTracker sessionTracker)
        {
            _config = config;
            _analytics = analytics;
            _eventBus = eventBus;
            _sessionTracker = sessionTracker;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                if (type != LogType.Error) return;
                if (_config == null || !_config.CrashIncludeErrors) return;
            }

            string preview = !string.IsNullOrEmpty(condition) && condition.Length > CONDITION_MAX_LEN
                ? condition.Substring(0, CONDITION_MAX_LEN)
                : condition ?? string.Empty;
            string stackHash = ComputeStackHash(stackTrace);

            if (!ShouldSend(stackHash)) return;

            // 메인 스레드 디스패치
            if (BackendMainThreadDispatcher.Instance != null)
                BackendMainThreadDispatcher.Instance.Enqueue(() => Emit(preview, stackHash, stackTrace));
            else
                Emit(preview, stackHash, stackTrace);
        }

        private void Emit(string preview, string stackHash, string fullStack)
        {
            if (_analytics == null || !_analytics.IsEnabled) return;

            var props = new Dictionary<string, object>
            {
                { PROP_CONDITION, preview },
                { PROP_STACK, stackHash },
            };
            if (_sessionTracker != null && !string.IsNullOrEmpty(_sessionTracker.CurrentSessionId))
                props[PROP_SESSION] = _sessionTracker.CurrentSessionId;

            if (Debug.isDebugBuild && _config != null && _config.CrashIncludeFullStackInDebugOnly && !string.IsNullOrEmpty(fullStack))
                props[PROP_FULL_STACK] = fullStack;

            _analytics.LogEvent(AnalyticsCategory.Error, ACTION, props);
            _eventBus?.Publish(new BackendCrashReportedEvent
            {
                ConditionPreview = preview,
                StackHash = stackHash,
            });
        }

        private bool ShouldSend(string stackHash)
        {
            if (string.IsNullOrEmpty(stackHash)) return true;
            lock (_lock)
            {
                if (_lastSent.TryGetValue(stackHash, out var last))
                {
                    if ((DateTime.UtcNow - last).TotalSeconds < DEDUP_TTL_SEC) return false;
                }
                _lastSent[stackHash] = DateTime.UtcNow;
                return true;
            }
        }

        private static string ComputeStackHash(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return "no-stack";
            try
            {
                using var sha1 = SHA1.Create();
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(stackTrace));
                var sb = new StringBuilder(STACK_HASH_LEN);
                for (int i = 0; i < hash.Length && sb.Length < STACK_HASH_LEN; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendCrashReporter] 스택 해시 예외: {e.Message}");
                return "hash-error";
            }
        }
    }
}
