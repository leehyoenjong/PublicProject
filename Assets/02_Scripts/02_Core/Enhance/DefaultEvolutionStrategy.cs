using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 진화 기본 전략. 확정 성공, 진화 단계 +1.
    /// 단계별 게이트: requiredGrade + requiredTranscendStep 충족 시 진행.
    /// </summary>
    public class DefaultEvolutionStrategy : IEnhanceStrategy
    {
        private readonly EnhanceDataCollection _collection;

        public DefaultEvolutionStrategy(EnhanceDataCollection collection)
        {
            _collection = collection;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeStage = target.EvolutionStage;
            target.EvolutionStage += 1;

            Debug.Log($"[강화] 진화 단계 증가: {beforeStage} → {target.EvolutionStage}");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Evolution,
                BeforeValue = beforeStage,
                AfterValue = target.EvolutionStage,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            EvolutionStageEntry entry = GetStageEntry(target.EvolutionStage);
            if (entry == null)
            {
                Debug.LogWarning($"[강화] 진화 단계 항목 없음: {target.EvolutionStage} (최대 도달)");
                return false;
            }

            if (target.Grade < (int)entry.RequiredGrade)
            {
                Debug.LogWarning($"[강화] 등급 부족: {(EquipmentGrade)target.Grade}/{entry.RequiredGrade}");
                return false;
            }

            if (target.TranscendStep < entry.RequiredTranscendStep)
            {
                Debug.LogWarning($"[강화] 초월 단계 부족: {target.TranscendStep}/{entry.RequiredTranscendStep}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            EvolutionStageEntry entry = GetStageEntry(target.EvolutionStage);
            if (entry == null)
            {
                return new EnhanceCost { Materials = System.Array.Empty<EnhanceMaterialEntry>(), CanAfford = false };
            }

            return new EnhanceCost
            {
                Materials = new[]
                {
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.Currency,
                        Amount = entry.Cost
                    },
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.EvolutionMaterial,
                        Amount = 1,
                        MaterialMID = entry.MaterialMID
                    }
                },
                CanAfford = true
            };
        }

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            return 1f;
        }

        private EvolutionStageEntry GetStageEntry(int stageIndex)
        {
            EnhanceData evolutionData = _collection != null ? _collection.Find(EnhanceType.Evolution) : null;
            return evolutionData != null ? evolutionData.FindEvolutionStage(stageIndex) : null;
        }
    }
}
