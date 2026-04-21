using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 투사체/히트박스 프리팹용 MonoBehaviour.
    /// 이동(선택)·lifespan·충돌 동작(DestroyOnHit/Pierce/Linger)·per-target 쿨다운·onHit/onExpire 스킬 실행.
    /// 콜라이더 크기/모양은 같은/자식 GameObject 의 BoxCollider2D/CircleCollider2D 에서 설정.
    /// 애니메이터로 GameObject/Collider 를 ON/OFF 하는 히트박스 스타일 공격도 동일 규약으로 동작.
    /// </summary>
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour, IProjectileInit
    {
        [Header("이동")]
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _lifespan = 3f;

        [Header("충돌 동작")]
        [SerializeField] private HitBehavior _hitBehavior = HitBehavior.DestroyOnHit;
        [SerializeField] private int _maxHits = 1;
        [SerializeField] private float _hitCooldownPerTarget = 0f;

        [Header("효과 스킬")]
        [SerializeField] private string _onHitSkillId;
        [SerializeField] private string _onExpireSkillId;

        private string _casterId;
        private int _level = 1;
        private float _powerMultiplier = 1f;

        private float _elapsed;
        private int _totalHits;
        private bool _expired;
        private readonly Dictionary<int, float> _lastHitByTarget = new Dictionary<int, float>();

        private ISkillSystem _skillSystem;
        private IObjectPoolManager _pool;
        private Rigidbody2D _rb;

        public void Initialize(string casterId, int level, float powerMultiplier)
        {
            _casterId = casterId;
            _level = level <= 0 ? 1 : level;
            _powerMultiplier = powerMultiplier <= 0f ? 1f : powerMultiplier;
            _elapsed = 0f;
            _totalHits = 0;
            _expired = false;
            _lastHitByTarget.Clear();
        }

        /// <summary>Spawn 액션이 onHitSkillId 를 런타임에 오버라이드할 수 있게 제공.</summary>
        public void SetOnHitSkillId(string skillId) => _onHitSkillId = skillId;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _skillSystem = ServiceLocator.Has<ISkillSystem>() ? ServiceLocator.Get<ISkillSystem>() : null;
            _pool = ServiceLocator.Has<IObjectPoolManager>() ? ServiceLocator.Get<IObjectPoolManager>() : null;
        }

        private void Update()
        {
            if (_expired) return;

            if (_speed > 0f)
            {
                Vector3 delta = transform.right * (_speed * Time.deltaTime);
                if (_rb != null) _rb.MovePosition(_rb.position + (Vector2)delta);
                else transform.position += delta;
            }

            if (_lifespan > 0f)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed >= _lifespan) Expire();
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_hitBehavior != HitBehavior.Linger && _hitCooldownPerTarget <= 0f) return;
            TryHit(other);
        }

        private void TryHit(Collider2D other)
        {
            if (_expired) return;
            if (other == null) return;

            int id = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetInstanceID()
                : other.GetInstanceID();

            if (_hitCooldownPerTarget > 0f
                && _lastHitByTarget.TryGetValue(id, out float last)
                && Time.time - last < _hitCooldownPerTarget)
            {
                return;
            }

            if (_maxHits > 0 && _totalHits >= _maxHits) return;

            string targetId = ResolveTargetId(other);
            if (!string.IsNullOrEmpty(_onHitSkillId) && _skillSystem != null)
            {
                _skillSystem.Execute(
                    _onHitSkillId,
                    _casterId,
                    targetId,
                    transform.position,
                    other.transform.position,
                    _level,
                    _powerMultiplier
                );
            }

            _totalHits++;
            _lastHitByTarget[id] = Time.time;

            if (_hitBehavior == HitBehavior.DestroyOnHit
                || (_hitBehavior == HitBehavior.Pierce && _maxHits > 0 && _totalHits >= _maxHits))
            {
                Expire();
            }
        }

        private static string ResolveTargetId(Collider2D other)
        {
            if (other == null) return string.Empty;
            var id = other.GetComponentInParent<IEntityId>();
            return id != null ? id.EntityId : other.gameObject.name;
        }

        private void Expire()
        {
            if (_expired) return;
            _expired = true;

            if (!string.IsNullOrEmpty(_onExpireSkillId) && _skillSystem != null)
            {
                _skillSystem.Execute(
                    _onExpireSkillId,
                    _casterId,
                    null,
                    transform.position,
                    transform.position,
                    _level,
                    _powerMultiplier
                );
            }

            if (_pool != null) _pool.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 충돌 대상이 엔티티 ID 를 제공하기 위한 얇은 인터페이스. 캐릭터 루트에 부착된 MonoBehaviour 가 구현.
    /// </summary>
    public interface IEntityId
    {
        string EntityId { get; }
    }
}
