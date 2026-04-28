using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 웨이브 한 단계. StageData._waves 자식 시트로 주입.
    /// monsters / bossPhases 는 인라인 배열.
    /// </summary>
    [Serializable]
    public class WaveData
    {
        [SerializeField] private WaveMonsterEntry[] _monsters;
        [SerializeField] private WaveTransitionCondition _transitionCondition;
        [SerializeField] private string _transitionTargetMonsterMID;
        [SerializeField] private float _transitionTimer;
        [SerializeField] private BossPhaseEntry[] _bossPhases;
        [SerializeField] private string _bossEntryHook;
        [SerializeField] private string _phaseTransitionHook;

        public IReadOnlyList<WaveMonsterEntry> Monsters => _monsters;
        public WaveTransitionCondition TransitionCondition => _transitionCondition;
        public string TransitionTargetMonsterMID => _transitionTargetMonsterMID;
        public float TransitionTimer => _transitionTimer;
        public IReadOnlyList<BossPhaseEntry> BossPhases => _bossPhases;
        public string BossEntryHook => _bossEntryHook;
        public string PhaseTransitionHook => _phaseTransitionHook;
    }
}
