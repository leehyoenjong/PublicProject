using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 펫 추종 어댑터. UnitController(Unit=IPetInfo) 가 부착된 GameObject 에 합성.
    /// PetInfo 의 FollowStrategy/FollowDistance/CatchUpDistance 를 PetFollowSolver 에 전달해 desiredPosition 산출,
    /// MoveTowards 로 이동. 오너의 이동 방향은 직전 프레임 위치 변화로 추정.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    [DisallowMultipleComponent]
    public class PetFollowAdapter : MonoBehaviour
    {
        [Header("오너 (필수 — 미설정 시 정지)")]
        [SerializeField] private Transform _owner;

        [Header("이동 속도")]
        [SerializeField] private float _normalSpeed = 4f;
        [SerializeField] private float _catchUpSpeed = 8f;

        [Header("Orbit 전략 회전 속도 (rad/s)")]
        [SerializeField] private float _orbitAngularSpeed = 1.5f;

        [Header("도착 임계 (이 이내면 정지)")]
        [SerializeField] private float _stoppingDistance = 0.05f;

        private UnitController _controller;
        private IPetInfo _petInfo;
        private Vector3 _ownerLastPos;
        private Vector2 _ownerFacing = Vector2.right;
        private float _orbitAngleRad;

        public Transform Owner
        {
            get => _owner;
            set
            {
                _owner = value;
                if (_owner != null) _ownerLastPos = _owner.position;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<UnitController>();
        }

        private void Start()
        {
            if (_controller == null || _controller.Unit == null)
            {
                Debug.LogWarning("[PetFollowAdapter] UnitController 미해결 — 비활성", this);
                enabled = false;
                return;
            }
            _petInfo = _controller.Unit as IPetInfo;
            if (_petInfo == null)
            {
                Debug.LogWarning($"[PetFollowAdapter] 비-펫 유닛 ({_controller.Unit.UnitId}) — 비활성", this);
                enabled = false;
                return;
            }
            if (_owner != null) _ownerLastPos = _owner.position;
        }

        private void Update()
        {
            if (_owner == null || _petInfo == null) return;
            if (!_controller.IsAlive) return;

            Vector3 ownerNow = _owner.position;
            Vector3 ownerDelta = ownerNow - _ownerLastPos;
            if (ownerDelta.sqrMagnitude > 0.0001f)
            {
                _ownerFacing = new Vector2(ownerDelta.x, ownerDelta.y).normalized;
            }
            _ownerLastPos = ownerNow;

            _orbitAngleRad += _orbitAngularSpeed * Time.deltaTime;

            Vector3 desired = PetFollowSolver.ComputeDesiredPosition(
                _petInfo.FollowStrategy,
                ownerNow,
                _ownerFacing,
                _petInfo.FollowDistance,
                _orbitAngleRad);

            if ((desired - transform.position).sqrMagnitude <= _stoppingDistance * _stoppingDistance) return;

            bool catchUp = PetFollowSolver.ShouldCatchUp(transform.position, ownerNow, _petInfo.CatchUpDistance);
            float speed = catchUp ? _catchUpSpeed : _normalSpeed;

            transform.position = Vector3.MoveTowards(transform.position, desired, speed * Time.deltaTime);
        }
    }
}
