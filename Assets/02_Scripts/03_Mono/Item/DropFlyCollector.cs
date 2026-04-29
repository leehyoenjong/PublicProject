using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 처치 시 떨어지는 드롭의 비주얼 컨트롤러. delay 후 또는 외부 호출(Collect) 즉시 target 으로 fly → AddItem.
    /// 변형 통합:
    ///  - 즉시 fly: collectDelay = 0
    ///  - 바닥 머문 후 fly: collectDelay > 0
    ///  - 스테이지 종료 일괄 회수: 외부에서 Collect() 강제 호출
    /// 외부 spawner 가 Bind(mid, count, target) 으로 동적 설정.
    /// </summary>
    [DisallowMultipleComponent]
    public class DropFlyCollector : MonoBehaviour
    {
        [Header("드롭 정보 (Bind 로 동적 설정 가능)")]
        [SerializeField] private int _itemMID;
        [SerializeField] private int _count = 1;
        [SerializeField] private Transform _target;

        [Header("타이밍")]
        [SerializeField, Min(0f)] private float _collectDelay = 1f;
        [SerializeField, Min(0f)] private float _flyDuration = 0.6f;
        [SerializeField, Min(0f)] private float _arcHeight = 1f;

        [Header("스폰 시 자동 시작")]
        [SerializeField] private bool _autoCollectOnStart = true;

        private IInventorySystem _inventory;
        private bool _flying;
        private bool _collected;

        public bool IsCollected => _collected;

        public void Bind(int itemMID, int count, Transform target)
        {
            _itemMID = itemMID;
            _count = count;
            _target = target;
        }

        private void Awake()
        {
            _inventory = ServiceLocator.Has<IInventorySystem>() ? ServiceLocator.Get<IInventorySystem>() : null;
        }

        private void Start()
        {
            if (_autoCollectOnStart) StartCoroutine(CollectRoutine());
        }

        /// <summary>외부에서 강제로 회수 시작. 이미 fly 중이거나 완료면 무시.</summary>
        public void Collect()
        {
            if (_flying || _collected) return;
            StopAllCoroutines();
            StartCoroutine(FlyRoutine());
        }

        private IEnumerator CollectRoutine()
        {
            if (_collectDelay > 0f) yield return new WaitForSeconds(_collectDelay);
            yield return FlyRoutine();
        }

        private IEnumerator FlyRoutine()
        {
            if (_target == null)
            {
                Debug.LogWarning("[DropFlyCollector] _target 미설정 — 즉시 회수 처리", this);
                CompletePickup();
                yield break;
            }

            _flying = true;
            Vector3 start = transform.position;
            float duration = _flyDuration > 0f ? _flyDuration : 0.001f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = FlyTrajectory.Evaluate(start, _target.position, t, _arcHeight);
                yield return null;
            }
            transform.position = _target.position;
            CompletePickup();
        }

        private void CompletePickup()
        {
            if (_collected) return;
            _collected = true;
            _flying = false;

            if (_inventory != null && _itemMID > 0 && _count > 0)
            {
                _inventory.AddItem(_itemMID, _count, "DropFlyCollector");
            }
            else if (_inventory == null)
            {
                Debug.LogWarning("[DropFlyCollector] IInventorySystem 미등록 — AddItem 생략", this);
            }

            Destroy(gameObject);
        }
    }
}
