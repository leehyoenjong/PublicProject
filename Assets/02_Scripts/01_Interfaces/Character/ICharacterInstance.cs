using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 런타임. IUnitInstance 공통 + 캐릭터 고유(Info/각성/등급/장착 스킬).
    /// </summary>
    public interface ICharacterInstance : IUnitInstance
    {
        ICharacterInfo Info { get; }
        int Awakening { get; }
        Rarity Rarity { get; }
        IReadOnlyList<SkillData> EquippedSkills { get; }
    }
}
