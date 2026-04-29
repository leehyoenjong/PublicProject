using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 등급 승급 기본 전략. 확률 기반 + 천장 시스템 + 보호권/축복/연속시도(Phase 2-B) hook.
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
            EnhanceData gradeData = GetGradeData();
            GradePolicyEntry policy = GetPolicy(target.Grade);
            float baseProb = policy != null ? policy.PromotionProb : 0f;
            int maxPity = policy != null ? policy.PromotionMaxPity : 0;

            float effectiveProb = ComputeEffectiveProb(baseProb, gradeData, context);

            bool success = _probabilityModel.Roll(effectiveProb, target.PityCount, maxPity);

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
            EnhanceFailPolicy basePolicy = policy != null ? policy.PromotionFailPolicy : EnhanceFailPolicy.Keep;
            EnhanceFailPolicy effectivePolicy = context.UseProtectionTicket ? EnhanceFailPolicy.Keep : basePolicy;

            ApplyFailPolicy(target, effectivePolicy);

            Debug.Log($"[GradePromotion] Failed. Pity: {target.PityCount}/{maxPity} Policy: {effectivePolicy} (base: {basePolicy}, ticket: {context.UseProtectionTicket})");

            return new EnhanceResult
            {
                IsSuccess = false,
                Type = EnhanceType.Grade,
                BeforeValue = beforeGrade,
                AfterValue = target.Grade,
                FailPolicy = effectivePolicy,
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

            if (context.UseProtectionTicket)
            {
                EnhanceData gradeData = GetGradeData();
                int ticketCost = gradeData != null ? gradeData.ProtectionTicketCost : 0;
                return new EnhanceCost
                {
                    Materials = new[]
                    {
                        new EnhanceMaterialEntry
                        {
                            MaterialType = EnhanceMaterialType.PromotionItem,
                            Amount = cost
                        },
                        new EnhanceMaterialEntry
                        {
                            MaterialType = EnhanceMaterialType.ProtectionTicket,
                            Amount = ticketCost
                        }
                    },
                    CanAfford = true
                };
            }

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
            EnhanceData gradeData = GetGradeData();
            GradePolicyEntry policy = GetPolicy(target.Grade);
            float baseProb = policy != null ? policy.PromotionProb : 0f;
            int maxPity = policy != null ? policy.PromotionMaxPity : 0;
            float effectiveProb = ComputeEffectiveProb(baseProb, gradeData, context);
            return _probabilityModel.GetDisplayProb(effectiveProb, target.PityCount, maxPity);
        }

        private EnhanceData GetGradeData()
        {
            return _collection != null ? _collection.Find(EnhanceType.Grade) : null;
        }

        private GradePolicyEntry GetPolicy(int grade)
        {
            EnhanceData gradeData = GetGradeData();
            return gradeData != null ? gradeData.FindGradePolicy(grade) : null;
        }

        private static float ComputeEffectiveProb(float baseProb, EnhanceData gradeData, EnhanceContext context)
        {
            float prob = baseProb;
            if (gradeData != null)
            {
                if (context.UseBlessing) prob += gradeData.BlessingBoost;
                if (context.ConsecutiveAttempts > 0) prob += gradeData.ConsecutiveBonusBase * context.ConsecutiveAttempts;
            }
            return Mathf.Clamp01(prob);
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
