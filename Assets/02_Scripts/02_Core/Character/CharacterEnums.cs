namespace PublicFramework
{
    public enum CharacterRole
    {
        Tank,
        Dealer,
        Healer,
        Support
    }

    public enum SkillSlotStrategy
    {
        Fixed,
        ByLevel,
        ByRarity,
        ByAwakening,
        Custom
    }

    public enum DialogueEvent
    {
        OnAcquire,
        OnLevelUp,
        OnWin,
        OnFormation,
        OnIdle
    }
}
