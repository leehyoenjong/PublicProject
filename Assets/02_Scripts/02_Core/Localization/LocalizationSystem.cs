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
        private readonly List<ILocalizationLoader> _loaders = new List<ILocalizationLoader>();

        private readonly Dictionary<LanguageCode, Dictionary<string, string>> _tables = new Dictionary<LanguageCode, Dictionary<string, string>>();
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
            Debug.Log($"[LocalizationSystem] Init started (language: {_currentLanguage})");
        }

        public void AddLoader(ILocalizationLoader loader)
        {
            _loaders.Add(loader);
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

            Debug.Log($"[LocalizationSystem] Table loaded: {table.Language} ({_tables[table.Language].Count} keys)");
        }

        public void LoadFromLoaders(LanguageCode language)
        {
            foreach (ILocalizationLoader loader in _loaders)
            {
                if (!loader.SupportsLanguage(language)) continue;

                Dictionary<string, string> data = loader.Load(language);
                if (data == null) continue;

                if (!_tables.TryGetValue(language, out Dictionary<string, string> existing))
                {
                    existing = new Dictionary<string, string>();
                    _tables[language] = existing;
                }

                foreach (var kvp in data)
                {
                    existing[kvp.Key] = kvp.Value;
                }

                if (!_supportedLanguages.Contains(language))
                {
                    _supportedLanguages.Add(language);
                }
            }
        }

        public void SetLanguage(LanguageCode language)
        {
            if (_currentLanguage == language) return;

            LanguageCode oldLanguage = _currentLanguage;
            _currentLanguage = language;

            if (!_tables.ContainsKey(language))
            {
                LoadFromLoaders(language);
            }

            SaveLanguage();

            _eventBus?.Publish(new LanguageChangedEvent
            {
                OldLanguage = oldLanguage,
                NewLanguage = language
            });

            Debug.Log($"[LocalizationSystem] Language changed: {oldLanguage} -> {language}");
        }

        public string GetText(string key, params object[] args)
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

                Debug.LogWarning($"[LocalizationSystem] Key not found: {key} ({_currentLanguage})");
                return key;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch (System.FormatException e)
                {
                    Debug.LogWarning($"[LocalizationSystem] Format error for key '{key}': {e.Message}");
                    return value;
                }
            }

            return value;
        }

        public bool HasKey(string key)
        {
            return FindText(key, _currentLanguage) != null
                || FindText(key, LanguageCode.En) != null
                || FindText(key, LanguageCode.Ko) != null;
        }

        public IReadOnlyList<LanguageCode> GetSupportedLanguages()
        {
            return _supportedLanguages.AsReadOnly();
        }

        private string FindText(string key, LanguageCode language)
        {
            if (_tables.TryGetValue(language, out Dictionary<string, string> table))
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
