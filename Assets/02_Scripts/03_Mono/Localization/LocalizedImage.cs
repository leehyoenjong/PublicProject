using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 언어별 이미지 교체 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizedImage : MonoBehaviour
    {
        [SerializeField] private LocalizedImageEntry[] _entries;

        private Image _image;
        private IEventBus _eventBus;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _eventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);

            ILocalizationSystem locSystem = ServiceLocator.Get<ILocalizationSystem>();
            UpdateImage(locSystem.CurrentLanguage);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            UpdateImage(evt.NewLanguage);
        }

        private void UpdateImage(LanguageCode language)
        {
            if (_image == null || _entries == null) return;

            foreach (LocalizedImageEntry entry in _entries)
            {
                if (entry.Language == language && entry.Sprite != null)
                {
                    _image.sprite = entry.Sprite;
                    return;
                }
            }
        }
    }

    [Serializable]
    public class LocalizedImageEntry
    {
        [SerializeField] private LanguageCode _language;
        [SerializeField] private Sprite _sprite;

        public LanguageCode Language => _language;
        public Sprite Sprite => _sprite;
    }
}
