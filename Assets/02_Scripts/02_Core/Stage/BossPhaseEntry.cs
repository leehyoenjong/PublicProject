using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 보스 페이즈 한 단계. WaveData._bossPhases 인라인 배열.
    /// hpThreshold 0~1 비율, 도달 시 patternHook 발화.
    /// </summary>
    [Serializable]
    public class BossPhaseEntry
    {
        [SerializeField] private float _hpThreshold;
        [SerializeField] private string _patternHook;

        public float HpThreshold => _hpThreshold;
        public string PatternHook => _patternHook;

        public BossPhaseEntry() { }

        public BossPhaseEntry(float hpThreshold, string patternHook)
        {
            _hpThreshold = hpThreshold;
            _patternHook = patternHook;
        }
    }
}
