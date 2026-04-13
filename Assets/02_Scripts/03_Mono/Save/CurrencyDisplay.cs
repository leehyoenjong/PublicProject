using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// SaveSystem에 저장된 재화 수치를 TMP에 표시.
    /// 프로젝트가 재화를 변경 후 CurrencyChangedEvent를 EventBus로 발행하면 자동 갱신.
    /// Refresh()로 수동 갱신도 가능.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private string _currencyKey;
        [SerializeField] private string _format = "{0:N0}";

        private TMP_Text _text;
        private ISaveSystem _saveSystem;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _saveSystem = ServiceLocator.Get<ISaveSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus?.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            Refresh();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        }

        public void SetKey(int slotIndex, string key)
        {
            _slotIndex = slotIndex;
            _currencyKey = key;
            Refresh();
        }

        public void Refresh()
        {
            if (_text == null || _saveSystem == null) return;
            if (string.IsNullOrEmpty(_currencyKey)) return;

            long value = 0;
            if (_saveSystem.HasKey(_slotIndex, _currencyKey))
            {
                value = _saveSystem.Load<long>(_slotIndex, _currencyKey);
            }

            _text.text = string.Format(_format, value);
        }

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.SlotIndex != _slotIndex) return;
            if (evt.Key != _currencyKey) return;

            if (_text != null)
            {
                _text.text = string.Format(_format, evt.NewValue);
            }
        }
    }
}
