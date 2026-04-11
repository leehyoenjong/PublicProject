using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 다국어 테이블 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewLocalizationTable", menuName = "PublicFramework/Localization/Table")]
    public class LocalizationTable : ScriptableObject
    {
        [SerializeField] private LanguageCode _language;
        [SerializeField] private LocalizationEntry[] _entries;

        public LanguageCode Language => _language;

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            if (_entries == null) return dict;

            foreach (LocalizationEntry entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    dict[entry.Key] = entry.Value;
                }
            }
            return dict;
        }
    }

    [Serializable]
    public class LocalizationEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private string _value;

        public string Key => _key;
        public string Value => _value;
    }
}
