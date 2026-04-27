namespace PublicFramework
{
    /// <summary>
    /// 프레임워크 기본 스탯 15종.
    /// 프로젝트 고유 스탯은 IStatContainer 의 커스텀 스탯 API(Dictionary&lt;string,float&gt;) 로 추가.
    /// </summary>
    public enum StatType
    {
        // 생존
        HP,            // 최대 체력 (현재 체력은 IStatContainer.CurrentHP 별도 보관)
        Defense,
        MagicResist,

        // 공격
        Attack,
        MagicPower,
        CritRate,
        CritDamage,

        // 기동
        AttackSpeed,
        MoveSpeed,
        Cooldown,      // 쿨다운 감소

        // 기타
        Evasion,
        HPRegen,
        MPRegen,

        // 저항
        ElementResist,
        StatusResist
    }
}
