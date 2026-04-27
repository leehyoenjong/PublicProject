namespace PublicFramework
{
    /// <summary>
    /// 4단계 계산식의 단계 식별자. 최종 = (Base + Flat) × (1 + Percent) × Multiplicative.
    /// Base 는 SetBaseValue / SetGrowthCurve API 로 설정하며 Modifier 의 Layer 가 아니다.
    /// </summary>
    public enum StatLayer
    {
        Flat,
        Percent,
        Multiplicative
    }

    /// <summary>
    /// Modifier 의 출처 분류. 분해 API(GetDecomposition) 에서 소스별 기여도를 보여줄 때 사용.
    /// </summary>
    public enum ModifierSource
    {
        Equipment,
        Buff,
        Skill,
        Other
    }

    /// <summary>
    /// 레벨에 따른 성장 커브 프리셋.
    /// </summary>
    public enum GrowthCurve
    {
        Linear,         // base + level × growth
        Quadratic,      // base + level² × growth
        Exponential,    // base × (1 + growth)^level
        Custom          // 코드 등록 공식 (StatContainer.RegisterCustomCurve)
    }
}
