namespace PublicFramework
{
    public struct StatChangedEvent
    {
        public string OwnerId;
        public StatType Type;
        public float OldValue;
        public float NewValue;
    }

    public struct ModifierAddedEvent
    {
        public string OwnerId;
        public StatType Type;
        public StatModType ModType;
        public float Value;
        public StatLayer Layer;
    }

    public struct ModifierRemovedEvent
    {
        public string OwnerId;
        public StatType Type;
        public StatLayer Layer;
    }
}
