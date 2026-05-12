namespace PublicFramework
{
    public struct BuffAppliedEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public int StackCount;
        public float Duration;
    }

    public struct BuffRemovedEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public string RemoveReason;
    }

    public struct BuffExpiredEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
    }

    public struct BuffStackChangedEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public int OldStack;
        public int NewStack;
    }

    public struct BuffRefreshedEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public float NewDuration;
    }

    public struct BuffTickEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public float TickValue;
        public int RemainingTicks;
    }

    public struct BuffImmuneEvent
    {
        public string TargetId;
        public string BuffId;
        public string SourceSkillId;
        public string Reason;
    }
}
