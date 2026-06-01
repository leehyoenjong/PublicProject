using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터용 BT/AI 어댑터. UnitController 와 같은 GameObject 에 부착해 합성한다.
    /// 책임: MonsterSystem 에 인스턴스 등록(Spawn) + 매 프레임 TickAI 호출.
    /// 타깃은 인스펙터의 Transform 슬롯에서 가져오며, 해당 Transform 에 UnitController 가 있으면 IUnit/Stats 동봉.
    /// UnitController 의 Awake 가 먼저 끝나야 InstanceId/Stats 가 유효하므로 Start 에서 Spawn.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    [DisallowMultipleComponent]
    public class MonsterAIAdapter : MonoBehaviour
    {
        [Header("AI Preset (선택 — 비어있으면 외부에서 사전 등록 가정)")]
        [SerializeField] private BehaviorTreePreset _aiPreset;

        [Header("타깃 (선택 — 없으면 BT 가 Failure 분기)")]
        [SerializeField] private Transform _target;

        [Header("자동 타겟 (수동 _target 비었을 때 적대 진영 자동 탐색)")]
        [SerializeField] private bool _autoAcquireTarget = true;
        [SerializeField] private float _retargetInterval = 0.5f;

        private UnitController _controller;
        private IMonsterSystem _monsterSystem;
        private IMonsterInstance _instance;
        private bool _spawned;
        private readonly ITargetSelector _targetSelector = NearestHostileTargetSelector.Instance;
        private float _retargetTimer;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void Awake()
        {
            _controller = GetComponent<UnitController>();
            _monsterSystem = ServiceLocator.Has<IMonsterSystem>() ? ServiceLocator.Get<IMonsterSystem>() : null;
        }

        private void Start()
        {
            if (_controller == null || _controller.Unit == null)
            {
                Debug.LogWarning("[몬스터AI] UnitController 미해결 — 비활성", this);
                enabled = false;
                return;
            }

            if (_controller.Unit is not IMonsterInfo)
            {
                Debug.LogWarning($"[몬스터AI] 비-몬스터 유닛 ({_controller.Unit.UnitId}) — 비활성", this);
                enabled = false;
                return;
            }

            if (_monsterSystem == null)
            {
                Debug.LogWarning("[몬스터AI] IMonsterSystem 미등록 — 비활성", this);
                enabled = false;
                return;
            }

            if (_aiPreset != null) _monsterSystem.RegisterAIPreset(_aiPreset);

            _instance = _monsterSystem.Spawn(
                _controller.Unit.UnitId,
                _controller.InstanceId,
                _controller.Stats,
                transform.position);

            _spawned = _instance != null;
            if (!_spawned)
            {
                Debug.LogWarning($"[몬스터AI] 스폰 실패 ({_controller.Unit.UnitId}/{_controller.InstanceId})", this);
            }
        }

        private void Update()
        {
            if (!_spawned || _monsterSystem == null) return;
            if (!_controller.IsAlive) return;

            if (_autoAcquireTarget) AutoAcquireTargetIfNeeded();

            IUnit targetUnit = null;
            Vector3 targetPos = Vector3.zero;
            IStatContainer targetStats = null;
            string targetInstanceId = null;

            if (_target != null)
            {
                UnitController tc = _target.GetComponent<UnitController>();
                if (tc != null)
                {
                    targetUnit = tc.Unit;
                    targetStats = tc.Stats;
                    targetInstanceId = tc.InstanceId;
                }
                targetPos = _target.position;
            }

            _monsterSystem.TickAI(_controller.InstanceId, Time.deltaTime, targetUnit, targetPos, targetStats, targetInstanceId, transform.position);

            // BT(MoveToTarget)가 Core 인스턴스 위치를 갱신했으면 transform 에 반영. z(2D 깊이)는 유지.
            if (_instance != null)
            {
                Vector3 moved = _instance.Position;
                moved.z = transform.position.z;
                transform.position = moved;
            }
        }

        /// <summary>
        /// 수동 _target 이 비었거나 죽었/비적대면 _retargetInterval 마다 가장 가까운 적대 유닛을 자동 지정.
        /// 매 프레임 전체 스캔을 피하려 인터벌로 제한(모바일 성능). 타겟 선택 규칙은 ITargetSelector 가 담당.
        /// </summary>
        private void AutoAcquireTargetIfNeeded()
        {
            if (IsCurrentTargetValid()) return;

            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer > 0f) return;
            _retargetTimer = _retargetInterval;

            UnitController[] all = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            UnitController picked = _targetSelector.Select(_controller, all);
            _target = picked != null ? picked.transform : null;
        }

        private bool IsCurrentTargetValid()
        {
            if (_target == null) return false;
            UnitController tc = _target.GetComponent<UnitController>();
            if (tc == null) return true; // 비-유닛 타깃(지점 등)은 유효로 간주
            return tc.IsAlive && FactionRules.IsHostile(_controller.Faction, tc.Faction);
        }
    }
}
