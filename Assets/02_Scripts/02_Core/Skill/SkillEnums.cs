namespace PublicFramework
{
    public enum SkillCategory
    {
        Active,
        Passive
    }

    public enum SkillTargetType
    {
        None,
        Self,
        Ally,
        Enemy,
        Ground
    }

    public enum SkillCostType
    {
        None,
        Mp,
        Hp
    }

    public enum SkillActionType
    {
        ApplyBuff,
        DealDamage,
        Heal,
        Spawn,
        Move,
        PlaySfx,
        PlayVfx
    }

    /// <summary>
    /// Projectile 충돌 동작.
    /// DestroyOnHit: 첫 히트에 파괴. Pierce: maxHits 까지 유지. Linger: lifespan 만료까지 체류.
    /// </summary>
    public enum HitBehavior
    {
        DestroyOnHit,
        Pierce,
        Linger
    }
}
