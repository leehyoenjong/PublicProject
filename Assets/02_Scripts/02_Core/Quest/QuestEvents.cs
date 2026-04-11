namespace PublicFramework
{
    public struct QuestRegisteredEvent
    {
        public string QuestId;
    }

    public struct QuestAcceptedEvent
    {
        public string QuestId;
        public QuestType QuestType;
    }

    public struct QuestCompletedEvent
    {
        public string QuestId;
        public QuestType QuestType;
    }

    public struct QuestRewardClaimedEvent
    {
        public string QuestId;
        public string RewardId;
        public RewardType RewardType;
        public int Amount;
    }

    public struct QuestAbandonedEvent
    {
        public string QuestId;
    }

    public struct QuestUnlockedEvent
    {
        public string QuestId;
        public QuestType QuestType;
    }

    public struct QuestResetEvent
    {
        public QuestType QuestType;
    }

    public struct QuestProgressEvent
    {
        public string QuestId;
        public string ConditionId;
        public int Current;
        public int Required;
    }
}
