namespace PublicFramework
{
    public struct StatChangedEvent
    {
        public string OwnerId;
        public StatType Type;
        public string CustomKey;       // 커스텀 스탯이면 키, 아니면 null
        public float OldValue;
        public float NewValue;
    }

    public struct ModifierAddedEvent
    {
        public string OwnerId;
        public StatType Type;
        public string CustomKey;
        public StatLayer Layer;
        public float Value;
        public ModifierSource SourceTag;
    }

    public struct ModifierRemovedEvent
    {
        public string OwnerId;
        public StatType Type;
        public string CustomKey;
        public StatLayer Layer;
    }
}
