using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 GameLog 기반 Analytics 구현.
    /// - opt-in: 기본 비활성. <see cref="IsEnabled"/>=false 시 호출 즉시 return (로그 없음 / API 호출 없음).
    /// - props 화이트리스트: string/int/long/bool/double 만 허용. 그 외 타입 거부 + LogWarning.
    /// - props 제한: 최대 16개 키, 키 길이 최대 40자 (뒤끝 GameLog 일반 제한 가정).
    /// - PII 금지: UserInDate/Nickname 등은 래퍼가 자동 포함하지 않는다. 호출부도 명시적으로 넣지 않아야 함.
    /// - fire-and-forget: 콜백 없이 이벤트로만 결과 전달(BackendAnalyticsLoggedEvent).
    /// </summary>
    public class BackendAnalytics : IBackendAnalytics
    {
        private const string ACTION_LOG = "AnalyticsLog";
        private const string ACTION_USER_PROP = "AnalyticsUserProperty";

        private const string PROP_ACTION = "action";
        private const string PROP_CATEGORY = "category";
        private const string PROP_LEVEL = "level";
        private const string PROP_PRODUCT_ID = "productId";
        private const string PROP_CURRENCY = "currency";
        private const string PROP_AMOUNT = "amount";

        private const int MAX_PROPS_COUNT = 16;
        private const int MAX_KEY_LENGTH = 40;

        private readonly IEventBus _eventBus;

        public bool IsEnabled { get; set; }

        public BackendAnalytics(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void LogEvent(AnalyticsCategory category, string action, IReadOnlyDictionary<string, object> props = null)
        {
            if (!IsEnabled) return;
            if (string.IsNullOrEmpty(action))
            {
                Debug.LogWarning("[BackendAnalytics] LogEvent 무시: action 비어있음");
                return;
            }

            var param = new Param();
            param.Add(PROP_CATEGORY, category.ToString());
            param.Add(PROP_ACTION, action);
            AppendWhitelistedProps(param, props);

            SendGameLog(category, action, param);
        }

        public void LogLevelUp(int level)
        {
            if (!IsEnabled) return;
            var param = new Param();
            param.Add(PROP_CATEGORY, AnalyticsCategory.Progress.ToString());
            param.Add(PROP_ACTION, "LevelUp");
            param.Add(PROP_LEVEL, level);
            SendGameLog(AnalyticsCategory.Progress, "LevelUp", param);
        }

        public void LogPurchase(string productId, string currency, decimal amount)
        {
            if (!IsEnabled) return;
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(currency))
            {
                Debug.LogWarning("[BackendAnalytics] LogPurchase 무시: productId/currency 비어있음");
                return;
            }

            var param = new Param();
            param.Add(PROP_CATEGORY, AnalyticsCategory.Economy.ToString());
            param.Add(PROP_ACTION, "Purchase");
            param.Add(PROP_PRODUCT_ID, productId);
            param.Add(PROP_CURRENCY, currency);
            // decimal → double 전환 (뒤끝 GameLog 숫자 타입 제한 가정).
            param.Add(PROP_AMOUNT, (double)amount);
            SendGameLog(AnalyticsCategory.Economy, "Purchase", param);
        }

        public void SetUserProperty(string key, string value)
        {
            if (!IsEnabled) return;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[BackendAnalytics] SetUserProperty 무시: key 비어있음");
                return;
            }
            if (key.Length > MAX_KEY_LENGTH)
            {
                Debug.LogWarning($"[BackendAnalytics] SetUserProperty 무시: key 길이 초과 ({key.Length}/{MAX_KEY_LENGTH})");
                return;
            }

            try
            {
                // 뒤끝 GameLog 에 user property 전용 테이블(예: "USER_PROPERTY") 을 사용.
                // 별도 Custom Table 설계가 필요할 수 있으며, 프로젝트별 조정 가능.
                var param = new Param();
                param.Add("key", key);
                param.Add("value", value ?? string.Empty);
                var bro = Backend.GameLog.InsertLogV2("USER_PROPERTY", param);
                if (!bro.IsSuccess())
                {
                    var err = BackendErrorMapper.Map(bro);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_USER_PROP, err, bro.GetMessage());
                    Debug.LogWarning($"[BackendAnalytics] SetUserProperty 실패: code={bro.GetStatusCode()}");
                }
                else
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendAnalytics] SetUserProperty 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_USER_PROP, BackendError.NetworkError, e.Message);
            }
        }

        private void SendGameLog(AnalyticsCategory category, string action, Param param)
        {
            try
            {
                var bro = Backend.GameLog.InsertLogV2(category.ToString(), param);
                bool ok = bro.IsSuccess();
                if (!ok)
                {
                    var err = BackendErrorMapper.Map(bro);
                    BackendEventDispatcher.PublishFailed(_eventBus, ACTION_LOG, err, bro.GetMessage());
                    Debug.LogWarning($"[BackendAnalytics] LogEvent 실패: category={category}, action={action}, code={bro.GetStatusCode()}");
                }
                else
                {
                    BackendEventDispatcher.NotifyOnlineIfRecovered(_eventBus);
                }

                _eventBus?.Publish(new BackendAnalyticsLoggedEvent
                {
                    Category = category,
                    Action = action,
                    Success = ok,
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendAnalytics] LogEvent 예외: {e.Message}");
                BackendEventDispatcher.PublishFailed(_eventBus, ACTION_LOG, BackendError.NetworkError, e.Message);
                _eventBus?.Publish(new BackendAnalyticsLoggedEvent
                {
                    Category = category,
                    Action = action,
                    Success = false,
                });
            }
        }

        private static void AppendWhitelistedProps(Param param, IReadOnlyDictionary<string, object> props)
        {
            if (props == null || props.Count == 0) return;

            int appended = 0;
            foreach (var kvp in props)
            {
                if (appended >= MAX_PROPS_COUNT)
                {
                    Debug.LogWarning($"[BackendAnalytics] props 개수 초과({MAX_PROPS_COUNT}) — 초과분 무시");
                    break;
                }

                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Length > MAX_KEY_LENGTH)
                {
                    Debug.LogWarning($"[BackendAnalytics] props 키 무시(빈 값 또는 길이 초과): key={kvp.Key}");
                    continue;
                }

                object value = kvp.Value;
                if (value == null)
                {
                    param.Add(kvp.Key, string.Empty);
                }
                else if (value is string s)
                {
                    param.Add(kvp.Key, s);
                }
                else if (value is int i)
                {
                    param.Add(kvp.Key, i);
                }
                else if (value is long l)
                {
                    param.Add(kvp.Key, l);
                }
                else if (value is bool b)
                {
                    param.Add(kvp.Key, b);
                }
                else if (value is double d)
                {
                    param.Add(kvp.Key, d);
                }
                else
                {
                    Debug.LogWarning($"[BackendAnalytics] props 타입 거부: key={kvp.Key}, type={value.GetType().Name}");
                    continue;
                }

                appended++;
            }
        }
    }
}
