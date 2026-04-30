using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 레벨 강화 기본 전략. 확정 성공, 레벨 +1.
    /// </summary>
    public class DefaultLevelEnhanceStrategy : IEnhanceStrategy
    {
        private readonly EnhanceDataCollection _collection;

        public DefaultLevelEnhanceStrategy(EnhanceDataCollection collection)
        {
            _collection = collection;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeLevel = target.Level;
            target.Level += 1;

            Debug.Log($"[강화] 레벨 증가: {beforeLevel} → {target.Level}");

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
            EnhanceData gradeData = _collection != null ? _collection.Find(EnhanceType.Grade) : null;
            if (gradeData == null)
            {
                Debug.LogWarning("[강화] 등급 데이터를 컬렉션에서 찾을 수 없음.");
                return false;
            }

            GradePolicyEntry policy = gradeData.FindGradePolicy(target.Grade);
            if (policy == null)
            {
                Debug.LogWarning($"[강화] 등급 정책 없음: {target.Grade}");
                return false;
            }

            if (target.Level >= policy.MaxLevel)
            {
                Debug.LogWarning($"[강화] 이미 최대 레벨: {target.Level}/{policy.MaxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            EnhanceData levelData = _collection != null ? _collection.Find(EnhanceType.Level) : null;
            if (levelData == null)
            {
                return new EnhanceCost { Materials = System.Array.Empty<EnhanceMaterialEntry>(), CanAfford = false };
            }

            int goldCost = Mathf.RoundToInt(levelData.LevelCostBase
                * Mathf.Pow(levelData.LevelCostMultiplier, target.Level)
                * (1 + target.Grade * levelData.GradeCostMultiplier));

            int stoneCost = Mathf.Max(1, Mathf.RoundToInt(levelData.StoneCostBase + target.Level * levelData.StoneCostMultiplier));

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
