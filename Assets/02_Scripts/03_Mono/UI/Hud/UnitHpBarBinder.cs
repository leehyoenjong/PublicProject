using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// UnitController 의 CurrentHP / MaxHP 비율을 Image.fillAmount 로 표시.
    /// IEventBus 의 UnitHpChangedEvent 를 구독해 _target.InstanceId 매칭 시 갱신.
    /// 화면 좌상단 HUD HP Bar 또는 World-Space HpBar 양쪽에서 재사용.
    /// </summary>
    public class UnitHpBarBinder : MonoBehaviour
    {
        [Header("타겟 (없으면 비활성)")]
        [SerializeField] private UnitController _target;

        [Header("UI (Image type=Filled, Method=Horizontal)")]
        [SerializeField] private Image _fillImage;

        [Header("연출")]
        [SerializeField, Range(0f, 30f)] private float _smoothSpeed = 12f;

        private IEventBus _eventBus;
        private float _displayedRatio = 1f;
        private float _targetRatio = 1f;

        public UnitController Target
        {
            get => _target;
            set
            {
                _target = value;
                RefreshImmediate();
            }
        }

        private void OnEnable()
        {
            if (_target == null)
                _target = GetComponentInParent<UnitController>();

            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            _eventBus?.Subscribe<UnitHpChangedEvent>(OnHpChanged);
            RefreshImmediate();
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<UnitHpChangedEvent>(OnHpChanged);
        }

        private void Update()
        {
            if (_fillImage == null) return;

            if (_smoothSpeed <= 0f)
            {
                _displayedRatio = _targetRatio;
            }
            else
            {
                _displayedRatio = Mathf.MoveTowards(_displayedRatio, _targetRatio, _smoothSpeed * Time.deltaTime);
            }
            _fillImage.fillAmount = _displayedRatio;
        }

        private void OnHpChanged(UnitHpChangedEvent evt)
        {
            if (_target == null || evt.InstanceId != _target.InstanceId) return;
            RecomputeTargetRatio();
        }

        private void RefreshImmediate()
        {
            RecomputeTargetRatio();
            _displayedRatio = _targetRatio;
            if (_fillImage != null) _fillImage.fillAmount = _displayedRatio;
        }

        private void RecomputeTargetRatio()
        {
            if (_target == null || _target.Stats == null)
            {
                _targetRatio = 0f;
                return;
            }

            float maxHp = _target.Stats.GetFinalValue(StatType.HP);
            if (maxHp <= 0f)
            {
                _targetRatio = 0f;
                return;
            }
            _targetRatio = Mathf.Clamp01(_target.Stats.CurrentHP / maxHp);
        }
    }
}
