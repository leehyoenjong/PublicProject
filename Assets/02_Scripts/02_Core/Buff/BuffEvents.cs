namespace PublicFramework
{
    public struct BuffAppliedEvent
    {
        public string TargetId;
        public string BuffId;
        public int StackCount;
        public float Duration;
    }

    public struct BuffRemovedEvent
    {
        public string TargetId;
        public string BuffId;
        public string RemoveReason;
    }

    public struct BuffExpiredEvent
    {
        public string TargetId;
        public string BuffId;
    }

    public struct BuffStackChangedEvent
    {
        public string TargetId;
        public string BuffId;
        public int OldStack;
        public int NewStack;
    }

    public struct BuffRefreshedEvent
    {
        public string TargetId;
        public string BuffId;
        public float NewDuration;
    }

    public struct BuffTickEvent
    {
        public string TargetId;
        public string BuffId;
        public float TickValue;
        public int RemainingTicks;
    }

    public struct BuffImmuneEvent
    {
        public string TargetId;
        public string BuffId;
        public string Reason;
    }
}
