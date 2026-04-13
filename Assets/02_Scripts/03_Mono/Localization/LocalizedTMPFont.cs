using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 언어별 TMP_FontAsset 자동 교체 컴포넌트
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMPFont : MonoBehaviour
    {
        [SerializeField] private TMPFontMapping _fontMapping;

        private TMP_Text _text;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _eventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);

            ILocalizationSystem locSystem = ServiceLocator.Get<ILocalizationSystem>();
            UpdateFont(locSystem.CurrentLanguage);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            UpdateFont(evt.NewLanguage);
        }

        private void UpdateFont(LanguageCode language)
        {
            if (_text == null || _fontMapping == null) return;

            TMP_FontAsset font = _fontMapping.GetFont(language);
            if (font != null)
            {
                _text.font = font;
            }
        }
    }
}
