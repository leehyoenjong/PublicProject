using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// SO 직렬화용 보유 스탯 DTO. 업적 티어/장비 베이스 스탯/세트 효과 등에서 사용.
    /// 런타임 적용 시 StatModifier 로 변환해 IStatContainer 에 주입.
    /// </summary>
    [Serializable]
    public class PassiveStat
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private StatLayer _layer;
        [SerializeField] private float _value;

        public StatType StatType => _statType;
        public StatLayer Layer => _layer;
        public float Value => _value;

        public PassiveStat() { }

        public PassiveStat(StatType statType, StatLayer layer, float value)
        {
            _statType = statType;
            _layer = layer;
            _value = value;
        }
    }
}
