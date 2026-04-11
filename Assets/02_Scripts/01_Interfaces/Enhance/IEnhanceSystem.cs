namespace PublicFramework
{
    /// <summary>
    /// 장비 강화 서비스 인터페이스.
    /// 타입별 전략 패턴으로 강화 로직을 위임한다.
    /// </summary>
    public interface IEnhanceSystem : IService
    {
        EnhanceResult Enhance(EquipmentInstanceData equipment, EnhanceContext context);
        bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context);
        EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context);
        float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context);
        void RegisterStrategy(EnhanceType type, IEnhanceStrategy strategy);
    }
}
