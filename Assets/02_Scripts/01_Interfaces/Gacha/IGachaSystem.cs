using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 시스템 서비스 인터페이스
    /// </summary>
    public interface IGachaSystem : IService
    {
        GachaResult Pull(string bannerId, int count = 1);
        GachaBannerData GetBannerInfo(string bannerId);
        IReadOnlyList<GachaBannerData> GetActiveBanners();
        PityCounter GetPityInfo(string bannerId);
        IReadOnlyList<DropEntry> GetProbabilities(string bannerId);
        void RegisterBanner(GachaBannerData bannerData);
        void UnregisterBanner(string bannerId);
        void SetPullStrategy(string bannerId, IPullStrategy strategy);
    }
}
