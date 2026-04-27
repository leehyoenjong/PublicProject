using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 펫 정의 계약. ItemData 하이브리드의 서브타입 SO 에 구현된다.
    /// IUnit 을 통해 캐릭터·몬스터와 같은 스킬·버프·데미지 로직을 공유하며, 펫 전용 훅(획득/장착/해제) 을 추가로 보유한다.
    /// 따라가기 4개 필드(전략/거리/복귀/충돌)는 Phase 2 Mono 에서 사용. 본 Phase 에서는 데이터 자리만 마련한다.
    /// </summary>
    public interface IPetInfo : IItemSubtypeInfo, IUnit
    {
        string MID { get; }
        int ItemMID { get; }
        int NameKey { get; }
        int DescKey { get; }
        string IconAddress { get; }
        PetRole Roles { get; }
        string ClassTag { get; }
        string ElementTag { get; }
        IReadOnlyList<SkillData> BaseSkills { get; }
        int SkillSlotMax { get; }
        string AIPresetMID { get; }

        PetFollowStrategy FollowStrategy { get; }
        float FollowDistance { get; }
        float CatchUpDistance { get; }
        PetCollisionPolicy CollisionPolicy { get; }

        IReadOnlyList<string> OnAcquireEvents { get; }
        IReadOnlyList<string> OnEquipEvents { get; }
        IReadOnlyList<string> OnUnequipEvents { get; }
    }
}
