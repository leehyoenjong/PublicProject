using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ILocalizationSystem 구현체.
    /// Fallback: 현재 언어 → En → Ko → Key 반환.
    /// </summary>
    public class LocalizationSystem : ILocalizationSystem
    {
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;

        private readonly Dictionary<LanguageCode, Dictionary<int, string>> _tables = new Dictionary<LanguageCode, Dictionary<int, string>>();
        private readonly List<LanguageCode> _supportedLanguages = new List<LanguageCode>();

        private LanguageCode _currentLanguage;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_LANGUAGE = "localization_language";

        public LanguageCode CurrentLanguage => _currentLanguage;

        public LocalizationSystem(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _currentLanguage = LanguageCode.Ko;

            LoadSavedLanguage();
            Debug.Log($"[로컬라이즈] 초기화 시작 (언어: {_currentLanguage})");
        }

        public void LoadTable(LocalizationTable table)
        {
            if (table == null) return;

            _tables[table.Language] = table.ToDictionary();

            if (!_supportedLanguages.Contains(table.Language))
            {
                _supportedLanguages.Add(table.Language);
            }

            _eventBus?.Publish(new LocalizationLoadedEvent
            {
                Language = table.Language,
                KeyCount = _tables[table.Language].Count
            });

            Debug.Log($"[로컬라이즈] 테이블 로드됨: {table.Language} ({_tables[table.Language].Count}개 키)");
        }

        public void SetLanguage(LanguageCode language)
        {
            if (_currentLanguage == language) return;

            LanguageCode oldLanguage = _currentLanguage;
            _currentLanguage = language;

            if (!_tables.ContainsKey(language))
            {
                Debug.LogWarning($"[로컬라이즈] '{language}' 테이블 미로드 — 폴백 적용됨.");
            }

            SaveLanguage();

            _eventBus?.Publish(new LanguageChangedEvent
            {
                OldLanguage = oldLanguage,
                NewLanguage = language
            });

            Debug.Log($"[로컬라이즈] 언어 변경됨: {oldLanguage} → {language}");
        }

        public string GetText(int key, params object[] args)
        {
            string value = FindText(key, _currentLanguage);

            // Fallback: En → Ko → Key
            if (value == null && _currentLanguage != LanguageCode.En)
            {
                value = FindText(key, LanguageCode.En);
            }
            if (value == null && _currentLanguage != LanguageCode.Ko)
            {
                value = FindText(key, LanguageCode.Ko);
            }
            if (value == null)
            {
                _eventBus?.Publish(new LocalizationKeyMissingEvent
                {
                    Key = key,
                    Language = _currentLanguage
                });

                Debug.LogWarning($"[로컬라이즈] 키를 찾을 수 없음: {key} ({_currentLanguage})");
                return key.ToString();
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch (System.FormatException e)
                {
                    Debug.LogWarning($"[로컬라이즈] 포맷 오류 (키='{key}'): {e.Message}");
                    return value;
                }
            }

            return value;
        }

        public bool HasKey(int key)
        {
            return FindText(key, _currentLanguage) != null
                || FindText(key, LanguageCode.En) != null
                || FindText(key, LanguageCode.Ko) != null;
        }

        public IReadOnlyList<LanguageCode> GetSupportedLanguages()
        {
            return _supportedLanguages.AsReadOnly();
        }

        private string FindText(int key, LanguageCode language)
        {
            if (_tables.TryGetValue(language, out Dictionary<int, string> table))
            {
                if (table.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
            return null;
        }

        private void SaveLanguage()
        {
            if (_saveSystem == null) return;
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_LANGUAGE, (int)_currentLanguage);
        }

        private void LoadSavedLanguage()
        {
            if (_saveSystem == null) return;
            if (!_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_LANGUAGE)) return;

            int langCode = _saveSystem.Load<int>(SAVE_SLOT, SAVE_KEY_LANGUAGE);
            _currentLanguage = (LanguageCode)langCode;
        }
    }
}
