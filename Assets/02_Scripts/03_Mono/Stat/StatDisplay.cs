using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 엔티티 ID + StatType 지정만으로 TMP에 최종 스탯 값을 자동 표시.
    /// StatChangedEvent 구독으로 실시간 갱신.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class StatDisplay : MonoBehaviour
    {
        [SerializeField] private string _ownerId;
        [SerializeField] private StatType _statType;
        [SerializeField] private string _format = "{0:0}";

        private TMP_Text _text;
        private IStatSystem _statSystem;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _statSystem = ServiceLocator.Get<IStatSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<StatChangedEvent>(OnStatChanged);
            Refresh();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<StatChangedEvent>(OnStatChanged);
        }

        public void SetOwner(string ownerId)
        {
            _ownerId = ownerId;
            Refresh();
        }

        private void OnStatChanged(StatChangedEvent evt)
        {
            if (evt.OwnerId != _ownerId) return;
            if (evt.Type != _statType) return;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null || _statSystem == null) return;
            if (string.IsNullOrEmpty(_ownerId)) return;

            IStatContainer container = _statSystem.GetContainer(_ownerId);
            if (container == null)
            {
                _text.text = string.Format(_format, 0f);
                return;
            }

            float value = container.GetFinalValue(_statType);
            _text.text = string.Format(_format, value);
        }
    }
}
