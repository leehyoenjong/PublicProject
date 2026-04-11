namespace PublicFramework
{
    public enum ModifierType
    {
        StatFlat,
        StatPercent,
        DamageOverTime,
        HealOverTime,
        Shield,
        StateFlag,
        Custom
    }

    public enum BuffCategory
    {
        Positive,
        Negative,
        Neutral
    }

    public enum BuffSource
    {
        Skill,
        Equipment,
        Item,
        Passive,
        Environment,
        System
    }

    public enum StackPolicy
    {
        None,
        Duration,
        Intensity,
        Independent
    }

    public enum DurationType
    {
        Timed,
        TurnBased,
        Permanent,
        /// <summary>
        /// 조건부 지속. Tick/Turn으로 만료되지 않으며,
        /// 외부에서 BuffInstance.MarkExpired()를 호출하여 수동으로 제거해야 한다.
        /// </summary>
        Conditional
    }

    public enum RefreshPolicy
    {
        Reset,
        Extend,
        Keep,
        Replace
    }
}
