using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 강화 설정.
    /// 등급별 최대 레벨, 승급 확률, 초월 보정, 각성 옵션 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "EnhanceConfig", menuName = "PublicFramework/Enhance/EnhanceConfig")]
    public class EnhanceConfig : ScriptableObject
    {
        [Header("레벨 강화")]
        [SerializeField] private int[] _maxLevelByGrade = { 10, 20, 30, 40, 50 };
        [SerializeField] private int _baseLevelUpCost = 100;
        [SerializeField] private float _levelCostMultiplier = 1.2f;
        [SerializeField] private float _gradeCostMultiplier = 0.5f;

        [Header("등급 승급")]
        [SerializeField] private float[] _promotionProbByGrade = { 1f, 0.8f, 0.5f, 0.2f };
        [SerializeField] private int[] _promotionMaxPityByGrade = { 0, 3, 5, 10 };
        [SerializeField] private int[] _promotionCostByGrade = { 5, 10, 20, 50 };
        [SerializeField] private EnhanceFailPolicy[] _promotionFailPolicyByGrade =
        {
            EnhanceFailPolicy.Keep,
            EnhanceFailPolicy.Keep,
            EnhanceFailPolicy.Keep,
            EnhanceFailPolicy.Keep
        };

        [Header("초월")]
        [SerializeField] private int _maxTranscendStep = 5;
        [SerializeField] private int _baseTranscendCost = 10;
        [SerializeField] private float _transcendCostMultiplier = 1.5f;

        [Header("레벨 강화 — 강화석")]
        [SerializeField] private int _baseStoneCost = 1;
        [SerializeField] private float _stoneCostMultiplier = 0.2f;

        [Header("각성")]
        [SerializeField] private int _baseAwakeningCost = 20;
        [SerializeField] private AwakeningOptionEntry[] _awakeningOptions =
        {
            new AwakeningOptionEntry { OptionId = "ATK_FLAT", MinValue = 5f, MaxValue = 15f, Weight = 30 },
            new AwakeningOptionEntry { OptionId = "DEF_FLAT", MinValue = 4f, MaxValue = 12f, Weight = 30 },
            new AwakeningOptionEntry { OptionId = "HP_FLAT", MinValue = 25f, MaxValue = 75f, Weight = 25 },
            new AwakeningOptionEntry { OptionId = "CRIT_RATE", MinValue = 0.02f, MaxValue = 0.08f, Weight = 10 },
            new AwakeningOptionEntry { OptionId = "CRIT_DMG", MinValue = 0.05f, MaxValue = 0.15f, Weight = 5 }
        };

        public int GetMaxLevel(int grade)
        {
            if (grade < 0 || grade >= _maxLevelByGrade.Length)
            {
                Debug.LogWarning($"[EnhanceConfig] Invalid grade for max level: {grade}");
                return _maxLevelByGrade[_maxLevelByGrade.Length - 1];
            }
            return _maxLevelByGrade[grade];
        }

        public int GetLevelUpCost(int currentLevel, int grade)
        {
            return Mathf.RoundToInt(_baseLevelUpCost * Mathf.Pow(_levelCostMultiplier, currentLevel) * (1 + grade * _gradeCostMultiplier));
        }

        public float GetPromotionProbability(int currentGrade)
        {
            if (currentGrade < 0 || currentGrade >= _promotionProbByGrade.Length)
            {
                return 0f;
            }
            return _promotionProbByGrade[currentGrade];
        }

        public int GetPromotionMaxPity(int currentGrade)
        {
            if (currentGrade < 0 || currentGrade >= _promotionMaxPityByGrade.Length)
            {
                return 0;
            }
            return _promotionMaxPityByGrade[currentGrade];
        }

        public int GetPromotionCost(int currentGrade)
        {
            if (currentGrade < 0 || currentGrade >= _promotionCostByGrade.Length)
            {
                return 0;
            }
            return _promotionCostByGrade[currentGrade];
        }

        public EnhanceFailPolicy GetPromotionFailPolicy(int currentGrade)
        {
            if (currentGrade < 0 || currentGrade >= _promotionFailPolicyByGrade.Length)
            {
                return EnhanceFailPolicy.Keep;
            }
            return _promotionFailPolicyByGrade[currentGrade];
        }

        public int GetMaxTranscendStep()
        {
            return _maxTranscendStep;
        }

        public int GetTranscendCost(int currentStep)
        {
            return Mathf.RoundToInt(_baseTranscendCost * Mathf.Pow(_transcendCostMultiplier, currentStep));
        }

        public int GetLevelUpStoneCost(int currentLevel)
        {
            return Mathf.Max(1, Mathf.RoundToInt(_baseStoneCost + currentLevel * _stoneCostMultiplier));
        }

        public int GetAwakeningCost(int slotIndex)
        {
            return _baseAwakeningCost * (1 + slotIndex);
        }

        public AwakeningOptionEntry[] GetAwakeningOptions()
        {
            var copy = new AwakeningOptionEntry[_awakeningOptions.Length];
            System.Array.Copy(_awakeningOptions, copy, _awakeningOptions.Length);
            return copy;
        }
    }

    [Serializable]
    public struct AwakeningOptionEntry
    {
        public string OptionId;
        public float MinValue;
        public float MaxValue;
        public int Weight;
    }
}
