namespace PublicFramework
{
    public enum QuestType
    {
        Main,
        Sub,
        Daily,
        Weekly,
        Event,
        Repeatable,
        Custom
    }

    public enum QuestState
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Rewarded
    }
}
