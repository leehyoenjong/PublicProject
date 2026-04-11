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

        public EnhanceResult Execute(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int beforeStep = equipment.TranscendStep;
            equipment.TranscendStep += 1;

            Debug.Log($"[Transcend] Step up: {beforeStep} → {equipment.TranscendStep}");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Transcend,
                BeforeValue = beforeStep,
                AfterValue = equipment.TranscendStep,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment.Grade < (int)EquipmentGrade.Legendary)
            {
                Debug.LogWarning($"[Transcend] Grade not Legendary: {(EquipmentGrade)equipment.Grade}");
                return false;
            }

            int maxLevel = _config.GetMaxLevel(equipment.Grade);
            if (equipment.Level < maxLevel)
            {
                Debug.LogWarning($"[Transcend] Level not max: {equipment.Level}/{maxLevel}");
                return false;
            }

            int maxStep = _config.GetMaxTranscendStep();

            if (equipment.TranscendStep >= maxStep)
            {
                Debug.LogWarning($"[Transcend] Already max step: {equipment.TranscendStep}/{maxStep}");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int cost = _config.GetTranscendCost(equipment.TranscendStep);

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

        public float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context)
        {
            return 1f;
        }
    }
}
