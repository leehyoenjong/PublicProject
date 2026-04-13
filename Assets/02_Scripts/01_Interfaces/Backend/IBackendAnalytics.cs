using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 뒤끝 Analytics(GameLog) 래퍼.
    /// opt-in 방식(GDPR). IsEnabled=false 이면 모든 호출이 즉시 무시된다.
    /// props 는 화이트리스트(string/int/long/bool/double) 타입만 허용되며, PII 는 포함 금지.
    /// </summary>
    public interface IBackendAnalytics : IService
    {
        bool IsEnabled { get; set; }

        void LogEvent(AnalyticsCategory category, string action, IReadOnlyDictionary<string, object> props = null);
        void LogLevelUp(int level);
        void LogPurchase(string productId, string currency, decimal amount);
        void SetUserProperty(string key, string value);
    }
}
