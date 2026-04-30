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
            Debug.Log("[광고] 더미 어댑터 초기화됨.");
            onSuccess?.Invoke();
        }

        public void LoadAd(AdType adType, string slotId)
        {
            _eventBus?.Publish(new AdLoadedEvent
            {
                SlotId = slotId,
                AdType = adType
            });

            Debug.Log($"[광고] 더미 광고 로드됨: {slotId} ({adType})");
        }

        public void ShowAd(AdType adType, string slotId, Action onSuccess, Action<AdFailReason> onFail)
        {
            Debug.Log($"[광고] 더미 광고 표시: {slotId} ({adType})");
            onSuccess?.Invoke();
        }

        public bool IsAdLoaded(AdType adType, string slotId)
        {
            return true;
        }

        public void ShowBanner(BannerPosition position)
        {
            Debug.Log($"[광고] 더미 배너 표시: {position}");
        }

        public void HideBanner()
        {
            Debug.Log("[광고] 더미 배너 숨김.");
        }
    }
}
