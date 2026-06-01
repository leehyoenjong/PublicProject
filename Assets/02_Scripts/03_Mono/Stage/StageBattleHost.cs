using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 런타임 진입점 (MonoBehaviour). StageSystem 과 EventBus 를 합성해 자동 진행.
    /// 책임: TryEnter / Tick / Wave 전환 평가 / 클리어 보고.
    /// 몬스터 스폰 자체는 본 컨트롤러 책임 외 — WaveStartedEvent 를 외부 spawner 가 함께 구독해 처리.
    /// 살아있는 몬스터 카운트는 WaveStartedEvent 시점의 WaveMonsterEntry 로 dict 초기화 + UnitDiedEvent 로 감산.
    /// </summary>
    [DisallowMultipleComponent]
    public class StageBattleHost : MonoBehaviour
    {
        [Header("스테이지 식별")]
        [SerializeField] private string _stageId;
        [SerializeField] private int _playerLevel = 1;

        [Header("진입 옵션")]
        [SerializeField] private bool _autoEnter;
        [SerializeField] private bool _isSweep;
        [SerializeField] private bool _autoBattle;
        [SerializeField] private string[] _partyCharacterIds;

        [Header("승리 보상 별점 (별점 평가기는 후속 트랙)")]
        [SerializeField, Range(0, 3)] private int _winStars = 3;

        private IStageSystem _stageSystem;
        private IEventBus _eventBus;
        private StageInstance _instance;
        private readonly Dictionary<string, int> _aliveByMID = new Dictionary<string, int>();
        private float _waveElapsed;
        private bool _entered;

        private Action<WaveStartedEvent> _onWaveStarted;
        private Action<UnitDiedEvent> _onUnitDied;
        private Action<StageClearedEvent> _onStageCleared;
        private Action<StageFailedEvent> _onStageFailed;

        public string StageId => _stageId;
        public bool Entered => _entered;
        public StageInstance Instance => _instance;

        private void Awake()
        {
            _stageSystem = ServiceLocator.Has<IStageSystem>() ? ServiceLocator.Get<IStageSystem>() : null;
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
        }

        private void OnEnable()
        {
            if (_eventBus == null) return;
            _onWaveStarted = OnWaveStarted;
            _onUnitDied = OnUnitDied;
            _onStageCleared = OnStageCleared;
            _onStageFailed = OnStageFailed;
            _eventBus.Subscribe(_onWaveStarted);
            _eventBus.Subscribe(_onUnitDied);
            _eventBus.Subscribe(_onStageCleared);
            _eventBus.Subscribe(_onStageFailed);
        }

        private void OnDisable()
        {
            if (_eventBus == null) return;
            if (_onWaveStarted != null) _eventBus.Unsubscribe(_onWaveStarted);
            if (_onUnitDied != null) _eventBus.Unsubscribe(_onUnitDied);
            if (_onStageCleared != null) _eventBus.Unsubscribe(_onStageCleared);
            if (_onStageFailed != null) _eventBus.Unsubscribe(_onStageFailed);
        }

        private void Start()
        {
            if (_autoEnter) Enter();
        }

        public bool Enter()
        {
            if (_stageSystem == null)
            {
                Debug.LogWarning("[전투호스트] IStageSystem 미등록 — 진입 무시", this);
                return false;
            }
            // 씬 간 선택(IStageSelection)이 있으면 그 스테이지로 진입 — 없으면 직렬화 기본값(_stageId) 폴백.
            string resolvedStageId = ResolveStageId();
            if (string.IsNullOrEmpty(resolvedStageId))
            {
                Debug.LogWarning("[전투호스트] _stageId 미설정 — 진입 무시", this);
                return false;
            }
            _stageId = resolvedStageId; // 이후 이벤트 필터·GetInstance 가 선택된 스테이지로 일관되게 동작

            var ctx = new StageContext
            {
                StageId = _stageId,
                IsSweep = _isSweep,
                AutoBattle = _autoBattle,
                PartyCharacterIds = _partyCharacterIds,
            };

            bool ok = _stageSystem.TryEnter(ctx, _playerLevel);
            if (!ok)
            {
                Debug.LogWarning($"[전투호스트] 진입 실패: {_stageId}", this);
                return false;
            }

            _instance = _stageSystem.GetInstance(_stageId);
            _entered = _instance != null;
            _waveElapsed = 0f;
            return _entered;
        }

        private string ResolveStageId()
        {
            if (ServiceLocator.Has<IStageSelection>())
            {
                string selected = ServiceLocator.Get<IStageSelection>().SelectedStageId;
                if (!string.IsNullOrEmpty(selected)) return selected;
            }
            return _stageId;
        }

        private void Update()
        {
            if (!_entered || _stageSystem == null) return;

            _stageSystem.Tick(Time.deltaTime);
            _waveElapsed += Time.deltaTime;

            EvaluateTransition();
        }

        private void EvaluateTransition()
        {
            if (_instance?.Data?.Waves == null) return;

            int idx = _instance.CurrentWaveIndex;
            if (idx < 0 || idx >= _instance.Data.Waves.Count) return;

            WaveData wave = _instance.Data.Waves[idx];
            if (wave == null) return;

            bool transition = WaveTransitionEvaluator.ShouldTransition(
                wave.TransitionCondition,
                wave.TransitionTargetMonsterMID,
                wave.TransitionTimer,
                _aliveByMID,
                _waveElapsed);
            if (!transition) return;

            int prevIdx = idx;
            _stageSystem.ReportWaveCleared();

            if (_instance.CurrentWaveIndex == prevIdx)
            {
                _stageSystem.ReportStageWin(_winStars);
            }
        }

        private void OnWaveStarted(WaveStartedEvent ev)
        {
            if (ev.StageId != _stageId) return;

            // _instance 는 Enter() 의 TryEnter 호출 직후 set 되지만, TryEnter 내부에서 StartWave
            // → Publish(WaveStartedEvent) 가 동기 호출되므로 본 콜백 시점에 아직 null 일 수 있다.
            // ServiceLocator 로 한 번 더 lookup 해 race 회피.
            StageInstance instance = _instance ?? _stageSystem?.GetInstance(_stageId);
            if (instance == null) return;

            _waveElapsed = 0f;
            _aliveByMID.Clear();

            if (instance.Data?.Waves == null) return;
            if (ev.WaveIndex < 0 || ev.WaveIndex >= instance.Data.Waves.Count) return;

            WaveData wave = instance.Data.Waves[ev.WaveIndex];
            if (wave?.Monsters == null) return;

            foreach (WaveMonsterEntry m in wave.Monsters)
            {
                if (m == null || string.IsNullOrEmpty(m.MonsterMID) || m.Count <= 0) continue;
                int prev = _aliveByMID.TryGetValue(m.MonsterMID, out int c) ? c : 0;
                _aliveByMID[m.MonsterMID] = prev + m.Count;
            }
        }

        private void OnUnitDied(UnitDiedEvent ev)
        {
            if (string.IsNullOrEmpty(ev.UnitId)) return;
            if (!_aliveByMID.TryGetValue(ev.UnitId, out int c)) return;
            c--;
            if (c <= 0) _aliveByMID.Remove(ev.UnitId);
            else _aliveByMID[ev.UnitId] = c;
        }

        private void OnStageCleared(StageClearedEvent ev)
        {
            if (ev.StageId != _stageId) return;
            _entered = false;
            _aliveByMID.Clear();
        }

        private void OnStageFailed(StageFailedEvent ev)
        {
            if (ev.StageId != _stageId) return;
            _entered = false;
            _aliveByMID.Clear();
        }
    }
}
