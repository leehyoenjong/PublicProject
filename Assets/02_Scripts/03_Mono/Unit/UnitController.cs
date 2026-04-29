using System;
using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터/몬스터/펫 공통 비주얼 컨트롤러. Prefab Variant 패턴 친화 — 부모 prefab 1개에 본 컴포넌트를 부착하고
    /// Variant 에서 _unitInfoAsset 슬롯과 Animator/Sprite 만 교체하면 도메인이 결정됨.
    /// 책임: Stats 컨테이너 바인드, Skill 이벤트 라우팅(Animation/Damage/Heal/Move), 사망 처리.
    /// 도메인 특화 행동(BT/AI, 펫 따라가기, PlayerInput) 은 별도 컴포넌트로 부착(Composition).
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitController : MonoBehaviour
    {
        [Header("데이터 (IUnit + BaseStatGroup 보유 SO)")]
        [SerializeField] private ScriptableObject _unitInfoAsset;
        [SerializeField] private int _initialLevel = 1;

        [Header("연출 (없으면 자식에서 자동 탐색)")]
        [SerializeField] private Animator _animator;

        private IUnit _unitInfo;
        private StatGroupData _baseStatGroup;
        private IStatContainer _stats;
        private IEventBus _eventBus;
        private IStatSystem _statSystem;

        // SkillSystem 만 lazy resolve — Initializer 와 Awake 순서 보장 없이도 안전.
        // (Stats/EventBus 는 Awake 에서 BindStats/SubscribeEvents 가 호출돼야 의미 있어 lazy 효과 없음 → 1.4 부팅 정비 후 정상화)
        private ISkillSystem SkillSystem
            => ServiceLocator.Has<ISkillSystem>() ? ServiceLocator.Get<ISkillSystem>() : null;
        private string _instanceId;
        private bool _isAlive = true;
        private Coroutine _moveCoroutine;

        private Action<SkillAnimationEvent> _onAnimation;
        private Action<SkillDamageEvent> _onDamage;
        private Action<SkillHealEvent> _onHeal;
        private Action<SkillMoveRequestedEvent> _onMove;

        public string InstanceId => _instanceId;
        public IUnit Unit => _unitInfo;
        public IStatContainer Stats => _stats;
        public bool IsAlive => _isAlive;
        public Animator Animator => _animator;

        private void Awake()
        {
            if (!ResolveUnitInfo()) return;
            ResolveServices();
            BindStats();
            BindAnimator();
            SubscribeEvents();

            _eventBus?.Publish(new UnitSpawnedEvent
            {
                InstanceId = _instanceId,
                UnitId = _unitInfo.UnitId,
                Position = transform.position,
            });
            Debug.Log($"[UnitController] Spawned: {_instanceId} (unit={_unitInfo.UnitId})");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            _statSystem?.RemoveContainer(_instanceId);
        }

        private bool ResolveUnitInfo()
        {
            _unitInfo = _unitInfoAsset as IUnit;
            if (_unitInfo == null)
            {
                string name = _unitInfoAsset != null ? _unitInfoAsset.name : "null";
                Debug.LogError($"[UnitController] _unitInfoAsset 가 IUnit 미구현 ({name}) — 컨트롤러 비활성", this);
                enabled = false;
                return false;
            }

            _baseStatGroup = _unitInfo.BaseStatGroup;
            _instanceId = $"{_unitInfo.UnitId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            return true;
        }

        private void ResolveServices()
        {
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            _statSystem = ServiceLocator.Has<IStatSystem>() ? ServiceLocator.Get<IStatSystem>() : null;
        }

        private void BindStats()
        {
            if (_statSystem == null)
            {
                Debug.LogWarning("[UnitController] IStatSystem 미등록 — Stats 비활성", this);
                return;
            }

            _stats = _statSystem.CreateContainer(_instanceId, _initialLevel);

            if (_baseStatGroup == null)
            {
                Debug.LogWarning($"[UnitController] BaseStatGroup 미설정 ({_unitInfo.UnitId}) — Stats 비어있음", this);
                return;
            }

            foreach (var entry in _baseStatGroup.Entries)
            {
                LevelCurve curve = entry.ToLevelCurve();
                if (string.IsNullOrEmpty(entry.CustomKey))
                    _stats.SetGrowthCurve(entry.Stat, curve);
                else
                    _stats.SetGrowthCurve(entry.CustomKey, curve);
            }
            _stats.SetLevel(_initialLevel);
            _stats.ResetToMax();
        }

        private void BindAnimator()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void SubscribeEvents()
        {
            if (_eventBus == null) return;
            _onAnimation = OnSkillAnimation;
            _onDamage = OnSkillDamage;
            _onHeal = OnSkillHeal;
            _onMove = OnSkillMove;
            _eventBus.Subscribe(_onAnimation);
            _eventBus.Subscribe(_onDamage);
            _eventBus.Subscribe(_onHeal);
            _eventBus.Subscribe(_onMove);
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus == null) return;
            if (_onAnimation != null) _eventBus.Unsubscribe(_onAnimation);
            if (_onDamage != null) _eventBus.Unsubscribe(_onDamage);
            if (_onHeal != null) _eventBus.Unsubscribe(_onHeal);
            if (_onMove != null) _eventBus.Unsubscribe(_onMove);
        }

        // ── 이벤트 라우팅 ──────────────────────────────────────────
        private void OnSkillAnimation(SkillAnimationEvent ev)
        {
            string targetUnit = ev.TargetRole == "Self" ? ev.CasterId : ev.TargetId;
            if (targetUnit != _instanceId) return;
            if (_animator == null || string.IsNullOrEmpty(ev.AnimKey)) return;
            _animator.Play(ev.AnimKey, ev.Layer);
        }

        private void OnSkillDamage(SkillDamageEvent ev)
        {
            if (ev.TargetId != _instanceId) return;
            ApplyHpDelta(-ev.Amount, $"skill:{ev.SkillId}");
        }

        private void OnSkillHeal(SkillHealEvent ev)
        {
            if (ev.TargetId != _instanceId) return;
            ApplyHpDelta(ev.Amount, $"skill:{ev.SkillId}");
        }

        private void OnSkillMove(SkillMoveRequestedEvent ev)
        {
            if (ev.CasterId != _instanceId) return;
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveCoroutine(ev.Direction, ev.Distance, ev.Duration));
        }

        private IEnumerator MoveCoroutine(Vector3 direction, float distance, float duration)
        {
            if (duration <= 0f)
            {
                transform.Translate(direction * distance, Space.World);
                _moveCoroutine = null;
                yield break;
            }

            Vector3 start = transform.position;
            Vector3 end = start + direction * distance;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            transform.position = end;
            _moveCoroutine = null;
        }

        // ── 공개 API ──────────────────────────────────────────
        public bool CastSkill(string skillId, string targetInstanceId = null, int level = 1)
        {
            ISkillSystem skillSystem = SkillSystem;
            if (skillSystem == null)
            {
                Debug.LogWarning("[UnitController] ISkillSystem 미등록 — CastSkill 무시", this);
                return false;
            }
            return skillSystem.Cast(skillId, _instanceId, targetInstanceId, level);
        }

        public void TakeDirectDamage(float amount, string source = "direct")
        {
            if (amount <= 0f) return;
            ApplyHpDelta(-amount, source);
        }

        public void HealDirect(float amount, string source = "direct")
        {
            if (amount <= 0f) return;
            ApplyHpDelta(amount, source);
        }

        // ── 내부 ──────────────────────────────────────────
        private void ApplyHpDelta(float delta, string source)
        {
            bool wasAlive = _isAlive;
            _isAlive = UnitDamageRouter.Apply(_stats, _eventBus, _instanceId, _unitInfo.UnitId, _isAlive, delta, source);
            if (wasAlive && !_isAlive)
                Debug.Log($"[UnitController] Died: {_instanceId} (cause={source})");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_unitInfoAsset != null && _unitInfoAsset is not IUnit)
            {
                Debug.LogError($"[UnitController] _unitInfoAsset 는 IUnit 구현 SO 여야 함 (현재: {_unitInfoAsset.GetType().Name})", this);
                _unitInfoAsset = null;
            }
        }
#endif
    }
}
