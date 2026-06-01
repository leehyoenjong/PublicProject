using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// SO 직렬화용 보유 스탯 DTO. 업적 티어/장비 베이스 스탯/세트 효과 등에서 사용.
    /// 런타임 적용 시 StatModifier 로 변환해 IStatContainer 에 주입.
    ///
    /// 값 단위 규약 (레이어별로 다름 — 시트 작성 시 주의):
    ///  - Flat            : 절대 가산값. 예) 10 → +10
    ///  - Percent         : 비율. 1.0 = +100%, 0.1 = +10%, 0.01 = +1%.
    ///                      예) +15% 는 0.15 로 적는다. 15 로 적으면 +1500% 가 된다.
    ///  - Multiplicative  : 곱셈 인자. 1.5 = ×1.5, 1.0 = 변화 없음, 0.5 = 절반.
    /// 최종식: Final = (Base + Flat) × (1 + ΣPercent) × ΠMultiplicative.
    /// (단위 실수 가드레일은 BuffStatRangeValidator 참고.)
    /// </summary>
    [Serializable]
    public class PassiveStat
    {
        [SerializeField] private StatType _statType;
        [SerializeField, SheetAlias("modType")] private StatLayer _layer;
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
