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

        public EnhanceResult Execute(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int beforeGrade = equipment.Grade;
            float baseProb = _config.GetPromotionProbability(equipment.Grade);
            int maxPity = _config.GetPromotionMaxPity(equipment.Grade);

            bool success = _probabilityModel.Roll(baseProb, equipment.PityCount, maxPity);

            if (success)
            {
                bool wasPity = maxPity > 0 && equipment.PityCount >= maxPity - 1;
                equipment.Grade += 1;
                equipment.PityCount = 0;

                if (wasPity)
                {
                    _eventBus?.Publish(new PityReachedEvent
                    {
                        InstanceId = equipment.InstanceId,
                        EnhanceType = EnhanceType.Grade
                    });
                }

                Debug.Log($"[GradePromotion] Grade up: {beforeGrade} → {equipment.Grade}");

                return new EnhanceResult
                {
                    IsSuccess = true,
                    Type = EnhanceType.Grade,
                    BeforeValue = beforeGrade,
                    AfterValue = equipment.Grade,
                    FailPolicy = EnhanceFailPolicy.Keep
                };
            }

            equipment.PityCount += 1;
            EnhanceFailPolicy policy = _config.GetPromotionFailPolicy(equipment.Grade);

            ApplyFailPolicy(equipment, policy);

            Debug.Log($"[GradePromotion] Failed. Pity: {equipment.PityCount}/{maxPity} Policy: {policy}");

            return new EnhanceResult
            {
                IsSuccess = false,
                Type = EnhanceType.Grade,
                BeforeValue = beforeGrade,
                AfterValue = equipment.Grade,
                FailPolicy = policy,
                MaxPity = maxPity
            };
        }

        public bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int maxGrade = (int)EquipmentGrade.Legendary;

            if (equipment.Grade >= maxGrade)
            {
                Debug.LogWarning($"[GradePromotion] Already max grade: {(EquipmentGrade)equipment.Grade}");
                return false;
            }

            int maxLevel = _config.GetMaxLevel(equipment.Grade);
            if (equipment.Level < maxLevel)
            {
                Debug.LogWarning($"[GradePromotion] Level not max: {equipment.Level}/{maxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int cost = _config.GetPromotionCost(equipment.Grade);

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

        private void ApplyFailPolicy(EquipmentInstanceData equipment, EnhanceFailPolicy policy)
        {
            switch (policy)
            {
                case EnhanceFailPolicy.Keep:
                    break;
                case EnhanceFailPolicy.Decrease:
                    if (equipment.Grade > 0)
                    {
                        equipment.Grade -= 1;
                        Debug.Log($"[GradePromotion] Decrease applied: Grade → {equipment.Grade}");
                    }
                    break;
                case EnhanceFailPolicy.Reset:
                    equipment.Grade = 0;
                    Debug.Log("[GradePromotion] Reset applied: Grade → 0");
                    break;
                case EnhanceFailPolicy.Destroy:
                    equipment.Grade = -1;
                    Debug.LogWarning("[GradePromotion] Destroy applied: equipment marked for destruction");
                    break;
            }
        }

        public float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context)
        {
            float baseProb = _config.GetPromotionProbability(equipment.Grade);
            int maxPity = _config.GetPromotionMaxPity(equipment.Grade);
            return _probabilityModel.GetDisplayProb(baseProb, equipment.PityCount, maxPity);
        }
    }
}
