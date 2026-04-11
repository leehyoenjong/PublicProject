using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// CSV 기반 다국어 데이터 로더
    /// </summary>
    public class CsvLocalizationLoader : ILocalizationLoader
    {
        private readonly string _resourcePath;
        private readonly HashSet<LanguageCode> _checkedLanguages = new HashSet<LanguageCode>();
        private readonly HashSet<LanguageCode> _supportedLanguages = new HashSet<LanguageCode>();

        public CsvLocalizationLoader(string resourcePath = "Localization")
        {
            _resourcePath = resourcePath;
        }

        public Dictionary<string, string> Load(LanguageCode language)
        {
            string path = $"{_resourcePath}/{language}";
            TextAsset asset = Resources.Load<TextAsset>(path);

            if (asset == null)
            {
                Debug.LogWarning($"[CsvLoader] File not found: {path}");
                return null;
            }

            var dict = new Dictionary<string, string>();
            string[] lines = asset.text.Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith("#")) continue;

                int separatorIndex = trimmed.IndexOf(',');
                if (separatorIndex <= 0) continue;

                string key = trimmed.Substring(0, separatorIndex).Trim();
                string value = trimmed.Substring(separatorIndex + 1).Trim();

                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                dict[key] = value;
            }

            Debug.Log($"[CsvLoader] Loaded {dict.Count} entries for {language}");
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
}
