using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 개별 엔티티의 스탯을 보유하고 계산하는 인터페이스
    /// </summary>
    public interface IStatContainer
    {
        float GetFinalValue(StatType type);
        float GetBaseValue(StatType type);
        void SetBaseValue(StatType type, float value);
        void SetGrowthValue(StatType type, float value);
        void AddModifier(IStatModifier modifier);
        void RemoveModifier(IStatModifier modifier);
        int RemoveModifiersFromSource(object source);
        IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer);
        void RecalculateAll();
    }
}
