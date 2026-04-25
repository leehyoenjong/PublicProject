namespace PublicFramework
{
    /// <summary>
    /// 프레임워크 기본 전략 구현.
    /// Fixed: value / ByLevel: 1 + (level-1)/value / ByRarity: rarity*value + 1 /
    /// ByAwakening: awakening*value + 1 / Custom: 0 (프로젝트 Resolver 로 교체 전제).
    /// </summary>
    public class DefaultSkillSlotResolver : ISkillSlotResolver
    {
        public int Resolve(ICharacterInfo info, int level, int awakening, Rarity rarity)
        {
            if (info == null) return 0;
            int value = info.SlotValue;
            int safeLevel = level < 1 ? 1 : level;
            int safeAwakening = awakening < 0 ? 0 : awakening;

            return info.SlotStrategy switch
            {
                SkillSlotStrategy.Fixed => value < 0 ? 0 : value,
                SkillSlotStrategy.ByLevel => value <= 0 ? 1 : 1 + ((safeLevel - 1) / value),
                SkillSlotStrategy.ByRarity => ((int)rarity * (value < 0 ? 0 : value)) + 1,
                SkillSlotStrategy.ByAwakening => (safeAwakening * (value < 0 ? 0 : value)) + 1,
                SkillSlotStrategy.Custom => 0,
                _ => 0
            };
        }
    }
}
