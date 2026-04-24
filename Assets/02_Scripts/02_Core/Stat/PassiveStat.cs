using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// SO 직렬화용 보유 스탯 DTO. 업적 티어 등에서 "Claim 시 영구 적용" 대상으로 사용.
    /// 런타임 적용 시 StatModifier 로 변환해 StatSystem 에 주입.
    /// </summary>
    [Serializable]
    public class PassiveStat
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private StatModType _modType;
        [SerializeField] private float _value;

        public StatType StatType => _statType;
        public StatModType ModType => _modType;
        public float Value => _value;

        public PassiveStat() { }

        public PassiveStat(StatType statType, StatModType modType, float value)
        {
            _statType = statType;
            _modType = modType;
            _value = value;
        }
    }
}
