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

                Debug.Log($"[강화] 등급 승급: {beforeGrade} → {target.Grade}");

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

            Debug.Log($"[강화] 등급 승급 실패. 천장: {target.PityCount}/{maxPity} 정책: {effectivePolicy} (기본: {basePolicy}, 보호권: {context.UseProtectionTicket})");

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
                Debug.LogWarning($"[강화] 이미 최대 등급: {(EquipmentGrade)target.Grade}");
                return false;
            }

            GradePolicyEntry policy = GetPolicy(target.Grade);
            if (policy == null)
            {
                Debug.LogWarning($"[강화] 등급 정책 없음: {target.Grade}");
                return false;
            }

            if (target.Level < policy.MaxLevel)
            {
                Debug.LogWarning($"[강화] 레벨 최대 미달: {target.Level}/{policy.MaxLevel}");
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
                        Debug.Log($"[강화] 등급 감소 적용: Grade → {target.Grade}");
                    }
                    break;
                case EnhanceFailPolicy.Reset:
                    target.Grade = 0;
                    Debug.Log("[강화] 등급 초기화 적용: Grade → 0");
                    break;
                case EnhanceFailPolicy.Destroy:
                    target.Grade = -1;
                    Debug.LogWarning("[강화] 파괴 적용: 장비가 파괴 대상으로 표시됨.");
                    break;
            }
        }
    }
}
