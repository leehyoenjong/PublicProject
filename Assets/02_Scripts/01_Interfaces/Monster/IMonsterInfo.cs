using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 정의 계약. ItemData 와 무관(수집 대상 아님)하지만 IUnit 을 통해 캐릭터·펫과 같은 스킬·버프·데미지 로직을 공유한다.
    /// </summary>
    public interface IMonsterInfo : IUnit
    {
        string MID { get; }
        int NameKey { get; }
        int DescKey { get; }
        string IconAddress { get; }
        MonsterType Type { get; }
        string ClassTag { get; }
        string ElementTag { get; }
        IReadOnlyList<SkillData> BaseSkills { get; }
        string DropTableMID { get; }
        string AIPresetMID { get; }
        int Level { get; }
        int ExpReward { get; }
        int GoldReward { get; }
        IReadOnlyList<string> OnSpawnEvents { get; }
        IReadOnlyList<string> OnDeathEvents { get; }
        string HitReactionId { get; }
    }
}
