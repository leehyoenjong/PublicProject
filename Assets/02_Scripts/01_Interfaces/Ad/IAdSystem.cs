using System;

namespace PublicFramework
{
    /// <summary>
    /// 광고 서비스 인터페이스.
    /// 보상 광고, 전면 광고, 배너 관리.
    /// </summary>
    public interface IAdSystem : IService
    {
        void ShowAd(string slotId, Action onSuccess, Action<AdFailReason> onFail);
        bool CanShowAd(string slotId);
        int GetDailyWatchCount(string slotId);
        int GetRemainingWatches(string slotId);
        void ShowBanner(BannerPosition position);
        void HideBanner();
    }
}
