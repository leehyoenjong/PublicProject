using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// WaveStartedEvent 구독 후 wave.Monsters 를 ObjectPool 에서 꺼내 배치하는 외부 spawner.
    /// StageBattleHost 책임 외 — 같은 GameObject 또는 별도 GO 에 부착해 합성한다.
    /// 스폰된 몬스터의 MonsterAIAdapter._target 에 player Transform 자동 주입.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveMonsterSpawner : MonoBehaviour
    {
        [Header("스폰 대상 스테이지 (StageBattleHost 와 동일하게)")]
        [SerializeField] private string _stageId;

        [Header("스폰 포인트 (비어있으면 자기 transform.position + fallbackRadius 랜덤)")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private float _fallbackRadius = 1.5f;

        [Header("타깃 (선택 — 비어있으면 비-몬스터 UnitController 자동 탐색)")]
        [SerializeField] private Transform _targetOverride;

        private IEventBus _eventBus;
        private IStageSystem _stageSystem;
        private IObjectPoolManager _pool;
        private Action<WaveStartedEvent> _onWaveStarted;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        public string StageId
        {
            get => _stageId;
            set => _stageId = value;
        }

        private void Awake()
        {
            // EventBus 만 Awake 캐시(GameBootstrapper 가 [DefaultExecutionOrder(-1000)] 로 선등록).
            // IStageSystem / IObjectPoolManager 는 PoolInitializer 등 Awake 순서에 따라 늦게 등록될 수
            // 있으니 OnWaveStarted 시점에 lazy lookup 한다.
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
        }

        private void OnEnable()
        {
            if (_eventBus == null) return;
            _onWaveStarted = OnWaveStarted;
            _eventBus.Subscribe(_onWaveStarted);
        }

        private void OnDisable()
        {
            if (_eventBus == null || _onWaveStarted == null) return;
            _eventBus.Unsubscribe(_onWaveStarted);
        }

        private void OnWaveStarted(WaveStartedEvent ev)
        {
            if (ev.StageId != _stageId) return;
            if (_stageSystem == null) _stageSystem = ServiceLocator.Has<IStageSystem>() ? ServiceLocator.Get<IStageSystem>() : null;
            if (_pool == null) _pool = ServiceLocator.Has<IObjectPoolManager>() ? ServiceLocator.Get<IObjectPoolManager>() : null;
            if (_stageSystem == null || _pool == null)
            {
                Debug.LogWarning("[몬스터스폰] IStageSystem/IObjectPoolManager 미등록 — 웨이브 스폰 불가", this);
                return;
            }

            StageInstance inst = _stageSystem.GetInstance(_stageId);
            if (inst?.Data?.Waves == null) return;
            if (ev.WaveIndex < 0 || ev.WaveIndex >= inst.Data.Waves.Count) return;

            WaveData wave = inst.Data.Waves[ev.WaveIndex];
            if (wave?.Monsters == null) return;

            Transform target = ResolveTarget();
            int slot = 0;

            foreach (WaveMonsterEntry m in wave.Monsters)
            {
                if (m == null || string.IsNullOrEmpty(m.MonsterMID) || m.Count <= 0) continue;
                for (int i = 0; i < m.Count; i++)
                {
                    Vector3 pos = ResolveSpawnPosition(slot++);
                    GameObject go = _pool.Spawn(m.MonsterMID, pos, Quaternion.identity);
                    if (go == null)
                    {
                        Debug.LogWarning($"[몬스터스폰] 풀 스폰 실패: {m.MonsterMID}", this);
                        continue;
                    }
                    if (target != null)
                    {
                        MonsterAIAdapter ai = go.GetComponent<MonsterAIAdapter>();
                        if (ai != null) ai.Target = target;
                    }
                    _spawned.Add(go);
                    Debug.Log($"[몬스터스폰] {m.MonsterMID} #{i} 위치 {pos}");
                }
            }
        }

        private Vector3 ResolveSpawnPosition(int slot)
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                Transform p = _spawnPoints[slot % _spawnPoints.Length];
                if (p != null) return p.position;
            }
            Vector2 r = UnityEngine.Random.insideUnitCircle * _fallbackRadius;
            return transform.position + new Vector3(r.x, r.y, 0f);
        }

        private Transform ResolveTarget()
        {
            if (_targetOverride != null) return _targetOverride;

            UnitController[] all = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController c in all)
            {
                if (c == null || c.Unit == null) continue;
                if (c.Unit is IMonsterInfo) continue;
                return c.transform;
            }
            return null;
        }
    }
}
