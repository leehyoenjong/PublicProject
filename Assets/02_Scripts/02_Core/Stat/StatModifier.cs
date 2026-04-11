using System;

namespace PublicFramework
{
    /// <summary>
    /// IStatModifier 구현체 (불변 객체)
    /// </summary>
    public class StatModifier : IStatModifier
    {
        public StatType TargetStat { get; }
        public StatModType ModType { get; }
        public float Value { get; }
        public int Priority { get; }
        public StatLayer Layer { get; }
        public object Source { get; }

        public StatModifier(StatType targetStat, StatModType modType, float value,
            StatLayer layer, object source, int priority = 0)
        {
            TargetStat = targetStat;
            ModType = modType;
            Value = value;
            Layer = layer;
            Source = source;
            Priority = priority;
        }
    }
}
