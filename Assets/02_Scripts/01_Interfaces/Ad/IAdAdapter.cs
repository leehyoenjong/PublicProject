using System;

namespace PublicFramework
{
    /// <summary>
    /// 광고 네트워크 추상화 인터페이스.
    /// AdMob, Unity Ads 등 실제 광고 SDK를 추상화한다.
    /// </summary>
    public interface IAdAdapter
    {
        void Initialize(Action onSuccess, Action<string> onFail);
        void LoadAd(AdType adType, string slotId);
        void ShowAd(AdType adType, string slotId, Action onSuccess, Action<AdFailReason> onFail);
        bool IsAdLoaded(AdType adType, string slotId);
        void ShowBanner(BannerPosition position);
        void HideBanner();
    }
}
