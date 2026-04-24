using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 다국어 텍스트 컴포넌트. LanguageChangedEvent로 실시간 갱신.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField, LocalizationKey] private int _localizationKey;

        private Text _text;
        private ILocalizationSystem _locSystem;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void Start()
        {
            _locSystem = ServiceLocator.Get<ILocalizationSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            UpdateText();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        public void SetKey(int key)
        {
            _localizationKey = key;
            UpdateText();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (_text == null || _locSystem == null) return;
            _text.text = _locSystem.GetText(_localizationKey);
        }
    }
}
