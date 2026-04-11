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
        public int Points;
    }

    public struct AchievementTitleUnlockedEvent
    {
        public string AchievementId;
        public string Title;
    }

    public struct AchievementMilestoneClaimedEvent
    {
        public int MilestoneIndex;
        public int RequiredPoints;
    }

    public struct AchievementPointsChangedEvent
    {
        public int TotalPoints;
        public int AddedPoints;
    }
}
