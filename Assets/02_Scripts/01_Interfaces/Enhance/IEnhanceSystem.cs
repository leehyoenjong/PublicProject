namespace PublicFramework
{
    /// <summary>
    /// 강화 서비스 인터페이스. 대상은 IEnhanceable — 장비/캐릭터/펫 등 강화 가능 인스턴스 모두 처리.
    /// 타입별 전략 패턴으로 강화 로직을 위임한다.
    /// </summary>
    public interface IEnhanceSystem : IService
    {
        EnhanceResult Enhance(IEnhanceable target, EnhanceContext context);
        bool CanEnhance(IEnhanceable target, EnhanceContext context);
        EnhanceCost GetCost(IEnhanceable target, EnhanceContext context);
        float GetDisplayProbability(IEnhanceable target, EnhanceContext context);
        void RegisterStrategy(EnhanceType type, IEnhanceStrategy strategy);
    }
}
