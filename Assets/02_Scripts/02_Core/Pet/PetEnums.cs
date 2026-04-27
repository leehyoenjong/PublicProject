using System;

namespace PublicFramework
{
    /// <summary>
    /// 펫 역할. 복수 조합 가능([Flags]). 시트에서 콤마 구분으로 입력하면 파서가 비트 OR 로 합성한다.
    /// </summary>
    [Flags]
    public enum PetRole
    {
        None = 0,
        Battle = 1 << 0,
        StatBoost = 1 << 1,
        SpecialAbility = 1 << 2,
        Follower = 1 << 3,
        Farming = 1 << 4
    }

    /// <summary>펫 추종 전략. 실제 이동은 Phase 2 Mono 가 사용.</summary>
    public enum PetFollowStrategy
    {
        Behind,
        Side,
        Orbit,
        Aerial,
        Hover
    }

    /// <summary>펫 충돌 정책. Phase 2 Mono 가 사용.</summary>
    public enum PetCollisionPolicy
    {
        Ghost,
        Solid,
        PlayerOnly
    }
}
