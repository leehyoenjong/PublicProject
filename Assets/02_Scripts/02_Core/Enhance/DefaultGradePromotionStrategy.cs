using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 등급 승급 기본 전략. 확률 기반 + 천장 시스템.
    /// </summary>
    public class DefaultGradePromotionStrategy : IEnhanceStrategy
    {
        private readonly EnhanceDataCollection _collection;
        private readonly IProbabilityModel _probabilityModel;
        private readonly IEventBus _eventBus;

        public DefaultGradePromotionStrategy(EnhanceDataCollection collection, IProbabilityModel probabilityModel, IEventBus eventBus)
        {
            _collection = collection;
            _probabilityModel = probabilityModel;
            _eventBus = eventBus;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeGrade = target.Grade;
            GradePolicyEntry policy = GetPolicy(target.Grade);
            float baseProb = policy != null ? policy.PromotionProb : 0f;
            int maxPity = policy != null ? policy.PromotionMaxPity : 0;

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
            EnhanceFailPolicy failPolicy = policy != null ? policy.PromotionFailPolicy : EnhanceFailPolicy.Keep;

            ApplyFailPolicy(target, failPolicy);

            Debug.Log($"[GradePromotion] Failed. Pity: {target.PityCount}/{maxPity} Policy: {failPolicy}");

            return new EnhanceResult
            {
                IsSuccess = false,
                Type = EnhanceType.Grade,
                BeforeValue = beforeGrade,
                AfterValue = target.Grade,
                FailPolicy = failPolicy,
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

            GradePolicyEntry policy = GetPolicy(target.Grade);
            if (policy == null)
            {
                Debug.LogWarning($"[GradePromotion] No grade policy for grade {target.Grade}");
                return false;
            }

            if (target.Level < policy.MaxLevel)
            {
                Debug.LogWarning($"[GradePromotion] Level not max: {target.Level}/{policy.MaxLevel}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            GradePolicyEntry policy = GetPolicy(target.Grade);
            int cost = policy != null ? policy.PromotionCost : 0;

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

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            GradePolicyEntry policy = GetPolicy(target.Grade);
            float baseProb = policy != null ? policy.PromotionProb : 0f;
            int maxPity = policy != null ? policy.PromotionMaxPity : 0;
            return _probabilityModel.GetDisplayProb(baseProb, target.PityCount, maxPity);
        }

        private GradePolicyEntry GetPolicy(int grade)
        {
            EnhanceData gradeData = _collection != null ? _collection.Find(EnhanceType.Grade) : null;
            return gradeData != null ? gradeData.FindGradePolicy(grade) : null;
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
    }
}
