namespace PublicFramework
{
    public struct GachaPullStartEvent
    {
        public string BannerId;
        public int Count;
    }

    public struct GachaPullResultEvent
    {
        public string BannerId;
        public GachaReward[] Rewards;
        public int TotalPullCount;
    }

    public struct GachaPityReachedEvent
    {
        public string BannerId;
        public PityType PityType;
        public int PullCount;
    }

    public struct GachaPityResetEvent
    {
        public string BannerId;
        public int PreviousPullCount;
    }

    public struct GachaBannerOpenEvent
    {
        public string BannerId;
        public GachaType BannerType;
    }

    public struct GachaBannerCloseEvent
    {
        public string BannerId;
    }

    public struct GachaDuplicateEvent
    {
        public string BannerId;
        public string RewardId;
        public ItemGrade Grade;
        public DuplicatePolicy Policy;
    }
}
