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

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeLevel = target.Level;
            target.Level += 1;

            Debug.Log($"[LevelEnhance] Level up: {beforeLevel} → {target.Level}");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Level,
                BeforeValue = beforeLevel,
                AfterValue = target.Level,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            int maxLevel = _config.GetMaxLevel(target.Grade);

            if (target.Level >= maxLevel)
            {
                Debug.LogWarning($"[LevelEnhance] Already max level: {target.Level}/{maxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            int goldCost = _config.GetLevelUpCost(target.Level, target.Grade);
            int stoneCost = _config.GetLevelUpStoneCost(target.Level);

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

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            return 1f;
        }
    }
}
