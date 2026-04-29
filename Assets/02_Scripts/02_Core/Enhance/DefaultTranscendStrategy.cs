using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 초월 기본 전략. 확정 성공, 초월 단계 +1.
    /// </summary>
    public class DefaultTranscendStrategy : IEnhanceStrategy
    {
        private readonly EnhanceDataCollection _collection;

        public DefaultTranscendStrategy(EnhanceDataCollection collection)
        {
            _collection = collection;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int beforeStep = target.TranscendStep;
            target.TranscendStep += 1;

            Debug.Log($"[Transcend] Step up: {beforeStep} → {target.TranscendStep}");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Transcend,
                BeforeValue = beforeStep,
                AfterValue = target.TranscendStep,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            if (target.Grade < (int)EquipmentGrade.Legendary)
            {
                Debug.LogWarning($"[Transcend] Grade not Legendary: {(EquipmentGrade)target.Grade}");
                return false;
            }

            EnhanceData gradeData = _collection != null ? _collection.Find(EnhanceType.Grade) : null;
            GradePolicyEntry gradePolicy = gradeData != null ? gradeData.FindGradePolicy(target.Grade) : null;
            int maxLevel = gradePolicy != null ? gradePolicy.MaxLevel : 0;

            if (target.Level < maxLevel)
            {
                Debug.LogWarning($"[Transcend] Level not max: {target.Level}/{maxLevel}");
                return false;
            }

            int maxStep = GetMaxTranscendStep();
            if (target.TranscendStep >= maxStep)
            {
                Debug.LogWarning($"[Transcend] Already max step: {target.TranscendStep}/{maxStep}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            EnhanceData transcendData = _collection != null ? _collection.Find(EnhanceType.Transcend) : null;
            TranscendStepEntry step = transcendData != null ? transcendData.FindTranscendStep(target.TranscendStep) : null;

            int cost = step != null ? step.Cost : 0;
            int sameItemCount = step != null ? step.RequiredSameItemCount : 1;

            return new EnhanceCost
            {
                Materials = new[]
                {
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.TranscendItem,
                        Amount = cost
                    },
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.SameEquipment,
                        Amount = sameItemCount
                    }
                },
                CanAfford = true
            };
        }

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            return 1f;
        }

        private int GetMaxTranscendStep()
        {
            EnhanceData transcendData = _collection != null ? _collection.Find(EnhanceType.Transcend) : null;
            return transcendData != null ? transcendData.TranscendSteps.Count : 0;
        }
    }
}
