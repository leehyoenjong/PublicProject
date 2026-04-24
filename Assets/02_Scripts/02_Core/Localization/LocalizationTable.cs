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

        public Dictionary<int, string> ToDictionary()
        {
            var dict = new Dictionary<int, string>();
            if (_entries == null) return dict;

            foreach (LocalizationEntry entry in _entries)
            {
                dict[entry.Key] = entry.Value;
            }
            return dict;
        }

#if UNITY_EDITOR
        /// <summary>에디터 전용: 시트 임포터에서 테이블 내용을 일괄 교체.</summary>
        public void SetData(LanguageCode language, LocalizationEntry[] entries)
        {
            _language = language;
            _entries = entries ?? Array.Empty<LocalizationEntry>();
        }
#endif
    }

    [Serializable]
    public class LocalizationEntry
    {
        [SerializeField] private int _key;
        [SerializeField] private string _value;

        public int Key => _key;
        public string Value => _value;

        public LocalizationEntry() { }

        public LocalizationEntry(int key, string value)
        {
            _key = key;
            _value = value;
        }
    }
}
