namespace PublicFramework
{
    /// <summary>
    /// 강화 종류별 전략 인터페이스.
    /// EnhanceSystem이 타입에 따라 적절한 전략을 호출한다.
    /// </summary>
    public interface IEnhanceStrategy
    {
        EnhanceResult Execute(EquipmentInstanceData equipment, EnhanceContext context);
        bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context);
        EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context);
        float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context);
    }
}
