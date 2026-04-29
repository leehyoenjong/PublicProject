using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 각성 옵션 풀 엔트리. Enhance_각성옵션 시트가 parentId=enhance_awakening 로 EnhanceData._awakeningOptions 에 주입.
    /// 가중치(Weight)에 비례한 RNG 추첨, 결과값은 [MinValue, MaxValue] 범위 균일분포.
    /// </summary>
    [Serializable]
    public struct AwakeningOptionEntry
    {
        [SerializeField] private string _optionId;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private int _weight;

        public string OptionId { get => _optionId; set => _optionId = value; }
        public float MinValue { get => _minValue; set => _minValue = value; }
        public float MaxValue { get => _maxValue; set => _maxValue = value; }
        public int Weight { get => _weight; set => _weight = value; }
    }
}
