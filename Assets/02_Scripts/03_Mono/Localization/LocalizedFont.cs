using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 언어별 폰트 자동 교체 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedFont : MonoBehaviour
    {
        [SerializeField] private FontMapping _fontMapping;

        private Text _text;
        private IEventBus _eventBus;

        private void Awake()
        {
            _text = GetComponent<Text>();
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

            Font font = _fontMapping.GetFont(language);
            if (font != null)
            {
                _text.font = font;
            }
        }
    }
}
