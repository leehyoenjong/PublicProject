namespace PublicFramework
{
    /// <summary>
    /// 강화 종류별 전략 인터페이스.
    /// EnhanceSystem이 타입에 따라 적절한 전략을 호출한다. 대상은 IEnhanceable 추상 — 장비/캐릭터/펫 모두 통과.
    /// </summary>
    public interface IEnhanceStrategy
    {
        EnhanceResult Execute(IEnhanceable target, EnhanceContext context);
        bool CanEnhance(IEnhanceable target, EnhanceContext context);
        EnhanceCost GetCost(IEnhanceable target, EnhanceContext context);
        float GetDisplayProbability(IEnhanceable target, EnhanceContext context);
    }
}
