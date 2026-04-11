using System;

namespace PublicFramework
{
    /// <summary>
    /// 스탯 수정자 인터페이스 (장비, 버프 등에서 공유)
    /// </summary>
    public interface IStatModifier
    {
        StatType TargetStat { get; }
        StatModType ModType { get; }
        float Value { get; }
        int Priority { get; }
        StatLayer Layer { get; }
        object Source { get; }
    }
}
