namespace PublicFramework
{
    public enum AdType
    {
        Rewarded,
        Interstitial,
        Banner
    }

    public enum AdFailReason
    {
        NotLoaded,
        NetworkError,
        DailyLimitReached,
        CooldownActive,
        VIPExempt,
        AdapterError,
        UserCancelled
    }

    public enum BannerPosition
    {
        Top,
        Bottom
    }
}
