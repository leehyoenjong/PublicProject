using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 펫 런타임. IUnitInstance 공통 + 펫 고유(Info, 장착 스킬, 장착 슬롯).
    /// 장착 슬롯 인덱스는 미장착 상태를 -1 로 표현한다.
    /// </summary>
    public interface IPetInstance : IUnitInstance
    {
        IPetInfo Info { get; }
        int EquippedSlotIndex { get; }
        bool IsEquipped { get; }
        IReadOnlyList<SkillData> EquippedSkills { get; }
    }
}
