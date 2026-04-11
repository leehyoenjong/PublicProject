using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 레벨 강화 기본 전략. 확정 성공, 레벨 +1.
    /// </summary>
    public class DefaultLevelEnhanceStrategy : IEnhanceStrategy
    {
        private readonly EnhanceConfig _config;

        public DefaultLevelEnhanceStrategy(EnhanceConfig config)
        {
            _config = config;
        }

        public EnhanceResult Execute(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int beforeLevel = equipment.Level;
            equipment.Level += 1;

            Debug.Log($"[LevelEnhance] Level up: {beforeLevel} → {equipment.Level}");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Level,
                BeforeValue = beforeLevel,
                AfterValue = equipment.Level,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int maxLevel = _config.GetMaxLevel(equipment.Grade);

            if (equipment.Level >= maxLevel)
            {
                Debug.LogWarning($"[LevelEnhance] Already max level: {equipment.Level}/{maxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int goldCost = _config.GetLevelUpCost(equipment.Level, equipment.Grade);
            int stoneCost = _config.GetLevelUpStoneCost(equipment.Level);

            return new EnhanceCost
            {
                Materials = new[]
                {
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.Currency,
                        Amount = goldCost
                    },
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.Stone,
                        Amount = stoneCost
                    }
                },
                CanAfford = true
            };
        }

        public float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context)
        {
            return 1f;
        }
    }
}
