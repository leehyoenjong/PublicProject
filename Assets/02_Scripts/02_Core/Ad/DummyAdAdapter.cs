using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 개발용 더미 광고 어댑터. 즉시 성공 반환.
    /// </summary>
    public class DummyAdAdapter : IAdAdapter
    {
        private readonly IEventBus _eventBus;

        public DummyAdAdapter(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void Initialize(Action onSuccess, Action<string> onFail)
        {
            Debug.Log("[DummyAd] Initialized");
            onSuccess?.Invoke();
        }

        public void LoadAd(AdType adType, string slotId)
        {
            _eventBus?.Publish(new AdLoadedEvent
            {
                SlotId = slotId,
                AdType = adType
            });

            Debug.Log($"[DummyAd] Ad loaded: {slotId} ({adType})");
        }

        public void ShowAd(AdType adType, string slotId, Action onSuccess, Action<AdFailReason> onFail)
        {
            Debug.Log($"[DummyAd] Ad shown: {slotId} ({adType})");
            onSuccess?.Invoke();
        }

        public bool IsAdLoaded(AdType adType, string slotId)
        {
            return true;
        }

        public void ShowBanner(BannerPosition position)
        {
            Debug.Log($"[DummyAd] Banner shown: {position}");
        }

        public void HideBanner()
        {
            Debug.Log("[DummyAd] Banner hidden");
        }
    }
}
