namespace PublicFramework
{
    public enum ChapterType
    {
        Normal,
        Dungeon,
        Event,
        Raid
    }

    public enum StageType
    {
        Normal,
        Boss,
        Defense,
        Rush,
        Story,
        DailyDungeon,
        InfiniteTower,
        Raid,
        PvPArena,
        EventOnly
    }

    public enum StageState
    {
        Locked,
        Available,
        InProgress,
        Cleared
    }

    public enum SpawnPattern
    {
        Simultaneous,
        Sequential,
        Interval
    }

    public enum WaveTransitionCondition
    {
        AllKill,
        BossKill,
        Timer,
        SpecificKill
    }

    public enum StageWinCondition
    {
        AllKill,
        BossKill,
        Survive
    }

    public enum StageLoseCondition
    {
        AllDead,
        Timeout
    }

    public enum StageEventType
    {
        Treasure,
        PetRescue,
        TutorialTrigger,
        CutScene,
        ItemGrant,
        Dialogue,
        Custom
    }

    public enum StageEventTrigger
    {
        OnEnter,
        OnWaveStart,
        OnWaveEnd,
        OnAllClear,
        OnTimer,
        OnHpThreshold,
        Manual
    }
}
