using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// JSON 기반 다국어 데이터 로더
    /// </summary>
    public class JsonLocalizationLoader : ILocalizationLoader
    {
        private readonly string _resourcePath;
        private readonly HashSet<LanguageCode> _checkedLanguages = new HashSet<LanguageCode>();
        private readonly HashSet<LanguageCode> _supportedLanguages = new HashSet<LanguageCode>();

        public JsonLocalizationLoader(string resourcePath = "Localization")
        {
            _resourcePath = resourcePath;
        }

        public Dictionary<string, string> Load(LanguageCode language)
        {
            string path = $"{_resourcePath}/{language}";
            TextAsset asset = Resources.Load<TextAsset>(path);

            if (asset == null)
            {
                Debug.LogWarning($"[JsonLoader] File not found: {path}");
                return null;
            }

            var wrapper = JsonUtility.FromJson<JsonLocalizationWrapper>(asset.text);
            if (wrapper == null || wrapper.entries == null)
            {
                Debug.LogWarning($"[JsonLoader] Parse failed: {path}");
                return null;
            }

            var dict = new Dictionary<string, string>();
            foreach (JsonLocalizationEntry entry in wrapper.entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    dict[entry.key] = entry.value;
                }
            }

            Debug.Log($"[JsonLoader] Loaded {dict.Count} entries for {language}");
            return dict;
        }

        public bool SupportsLanguage(LanguageCode language)
        {
            if (_checkedLanguages.Contains(language))
            {
                return _supportedLanguages.Contains(language);
            }

            _checkedLanguages.Add(language);

            string path = $"{_resourcePath}/{language}";
            bool exists = Resources.Load<TextAsset>(path) != null;

            if (exists)
            {
                _supportedLanguages.Add(language);
            }

            return exists;
        }
    }

    /// <remarks>
    /// JsonUtility는 public 필드만 직렬화하므로 public 필드를 사용합니다.
    /// </remarks>
    [System.Serializable]
    internal class JsonLocalizationWrapper
    {
        public JsonLocalizationEntry[] entries;
    }

    /// <remarks>
    /// JsonUtility는 public 필드만 직렬화하므로 public 필드를 사용합니다.
    /// </remarks>
    [System.Serializable]
    internal class JsonLocalizationEntry
    {
        public string key;
        public string value;
    }
}
