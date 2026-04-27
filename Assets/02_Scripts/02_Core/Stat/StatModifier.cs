namespace PublicFramework
{
    /// <summary>
    /// IStatModifier 기본 구현. 가변 RemainingSeconds(임시 modifier)는 컨테이너의 Tick 이 감산.
    /// 임시 modifier 가 아닌 경우 IsTemporary=false, RemainingSeconds=0 으로 둔다.
    /// </summary>
    public class StatModifier : IStatModifier
    {
        public StatType TargetStat { get; }
        public string CustomKey { get; }
        public StatLayer Layer { get; }
        public float Value { get; }
        public object Source { get; }
        public ModifierSource SourceTag { get; }
        public string SourceLabel { get; }
        public int Priority { get; }
        public bool IsTemporary { get; }
        public float RemainingSeconds { get; private set; }

        public StatModifier(
            StatType targetStat,
            StatLayer layer,
            float value,
            object source = null,
            ModifierSource sourceTag = ModifierSource.Other,
            string sourceLabel = null,
            int priority = 0,
            string customKey = null,
            float durationSeconds = 0f)
        {
            TargetStat = targetStat;
            CustomKey = customKey;
            Layer = layer;
            Value = value;
            Source = source;
            SourceTag = sourceTag;
            SourceLabel = sourceLabel;
            Priority = priority;
            IsTemporary = durationSeconds > 0f;
            RemainingSeconds = IsTemporary ? durationSeconds : 0f;
        }

        public void DecrementTime(float deltaTime)
        {
            if (!IsTemporary) return;
            RemainingSeconds -= deltaTime;
            if (RemainingSeconds < 0f) RemainingSeconds = 0f;
        }

        public bool IsExpired => IsTemporary && RemainingSeconds <= 0f;
    }
}
