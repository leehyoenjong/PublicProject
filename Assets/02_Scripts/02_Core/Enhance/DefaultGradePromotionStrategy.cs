using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 등급 승급 기본 전략. 확률 기반 + 천장 시스템.
    /// </summary>
    public class DefaultGradePromotionStrategy : IEnhanceStrategy
    {
        private readonly EnhanceConfig _config;
        private readonly IProbabilityModel _probabilityModel;
        private readonly IEventBus _eventBus;

        public DefaultGradePromotionStrategy(EnhanceConfig config, IProbabilityModel probabilityModel, IEventBus eventBus)
        {
            _config = config;
            _probabilityModel = probabilityModel;
            _eventBus = eventBus;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeGrade = target.Grade;
            float baseProb = _config.GetPromotionProbability(target.Grade);
            int maxPity = _config.GetPromotionMaxPity(target.Grade);

            bool success = _probabilityModel.Roll(baseProb, target.PityCount, maxPity);

            if (success)
            {
                bool wasPity = maxPity > 0 && target.PityCount >= maxPity - 1;
                target.Grade += 1;
                target.PityCount = 0;

                if (wasPity)
                {
                    _eventBus?.Publish(new PityReachedEvent
                    {
                        InstanceId = target.InstanceId,
                        EnhanceType = EnhanceType.Grade
                    });
                }

                Debug.Log($"[GradePromotion] Grade up: {beforeGrade} → {target.Grade}");

                return new EnhanceResult
                {
                    IsSuccess = true,
                    Type = EnhanceType.Grade,
                    BeforeValue = beforeGrade,
                    AfterValue = target.Grade,
                    FailPolicy = EnhanceFailPolicy.Keep
                };
            }

            target.PityCount += 1;
            EnhanceFailPolicy policy = _config.GetPromotionFailPolicy(target.Grade);

            ApplyFailPolicy(target, policy);

            Debug.Log($"[GradePromotion] Failed. Pity: {target.PityCount}/{maxPity} Policy: {policy}");

            return new EnhanceResult
            {
                IsSuccess = false,
                Type = EnhanceType.Grade,
                BeforeValue = beforeGrade,
                AfterValue = target.Grade,
                FailPolicy = policy,
                MaxPity = maxPity
            };
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            int maxGrade = (int)EquipmentGrade.Legendary;

            if (target.Grade >= maxGrade)
            {
                Debug.LogWarning($"[GradePromotion] Already max grade: {(EquipmentGrade)target.Grade}");
                return false;
            }

            int maxLevel = _config.GetMaxLevel(target.Grade);
            if (target.Level < maxLevel)
            {
                Debug.LogWarning($"[GradePromotion] Level not max: {target.Level}/{maxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            int cost = _config.GetPromotionCost(target.Grade);

            return new EnhanceCost
            {
                Materials = new[]
                {
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.PromotionItem,
                        Amount = cost
                    }
                },
                CanAfford = true
            };
        }

        private void ApplyFailPolicy(IEnhanceable target, EnhanceFailPolicy policy)
        {
            switch (policy)
            {
                case EnhanceFailPolicy.Keep:
                    break;
                case EnhanceFailPolicy.Decrease:
                    if (target.Grade > 0)
                    {
                        target.Grade -= 1;
                        Debug.Log($"[GradePromotion] Decrease applied: Grade → {target.Grade}");
                    }
                    break;
                case EnhanceFailPolicy.Reset:
                    target.Grade = 0;
                    Debug.Log("[GradePromotion] Reset applied: Grade → 0");
                    break;
                case EnhanceFailPolicy.Destroy:
                    target.Grade = -1;
                    Debug.LogWarning("[GradePromotion] Destroy applied: equipment marked for destruction");
                    break;
            }
        }

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            float baseProb = _config.GetPromotionProbability(target.Grade);
            int maxPity = _config.GetPromotionMaxPity(target.Grade);
            return _probabilityModel.GetDisplayProb(baseProb, target.PityCount, maxPity);
        }
    }
}
