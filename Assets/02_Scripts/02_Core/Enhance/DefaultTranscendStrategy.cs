using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 초월 기본 전략. 확정 성공, 초월 단계 +1.
    /// </summary>
    public class DefaultTranscendStrategy : IEnhanceStrategy
    {
        private readonly EnhanceConfig _config;

        public DefaultTranscendStrategy(EnhanceConfig config)
        {
            _config = config;
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

            int maxLevel = _config.GetMaxLevel(target.Grade);
            if (target.Level < maxLevel)
            {
                Debug.LogWarning($"[Transcend] Level not max: {target.Level}/{maxLevel}");
                return false;
            }

            int maxStep = _config.GetMaxTranscendStep();

            if (target.TranscendStep >= maxStep)
            {
                Debug.LogWarning($"[Transcend] Already max step: {target.TranscendStep}/{maxStep}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            int cost = _config.GetTranscendCost(target.TranscendStep);

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
                        Amount = 1
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
