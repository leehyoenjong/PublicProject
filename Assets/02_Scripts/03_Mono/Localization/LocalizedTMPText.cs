using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// TMP_Text 다국어 컴포넌트. LanguageChangedEvent로 실시간 갱신.
    /// Format 인자는 _formatArgs에 채워 넣으면 GetText(key, args)로 전달.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMPText : MonoBehaviour
    {
        [SerializeField] private string _localizationKey;
        [SerializeField] private string[] _formatArgs;

        private TMP_Text _text;
        private ILocalizationSystem _locSystem;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
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

        public void SetKey(string key)
        {
            _localizationKey = key;
            UpdateText();
        }

        public void SetFormatArgs(params string[] args)
        {
            _formatArgs = args;
            UpdateText();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (_text == null || _locSystem == null) return;
            if (string.IsNullOrEmpty(_localizationKey)) return;

            if (_formatArgs != null && _formatArgs.Length > 0)
            {
                _text.text = _locSystem.GetText(_localizationKey, _formatArgs);
            }
            else
            {
                _text.text = _locSystem.GetText(_localizationKey);
            }
        }
    }
}
