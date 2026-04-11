namespace PublicFramework
{
    public struct AdStartEvent
    {
        public string SlotId;
        public AdType AdType;
    }

    public struct AdCompleteEvent
    {
        public string SlotId;
        public AdType AdType;
        public bool Rewarded;
    }

    public struct AdFailEvent
    {
        public string SlotId;
        public AdType AdType;
        public AdFailReason Reason;
    }

    public struct AdLoadedEvent
    {
        public string SlotId;
        public AdType AdType;
    }
}
