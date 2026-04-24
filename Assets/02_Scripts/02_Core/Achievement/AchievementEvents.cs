namespace PublicFramework
{
    public struct AchievementProgressEvent
    {
        public string AchievementId;
        public int CurrentAmount;
        public int RequiredAmount;
    }

    public struct AchievementCompletedEvent
    {
        public string AchievementId;
        public AchievementCategory Category;
        public int Tier;
    }

    public struct AchievementRewardClaimedEvent
    {
        public string AchievementId;
        public int Tier;
    }
}
