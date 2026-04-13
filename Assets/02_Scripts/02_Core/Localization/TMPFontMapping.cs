using System;
using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 언어별 TMP_FontAsset 매핑 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TMPFontMapping", menuName = "PublicFramework/Localization/TMPFontMapping")]
    public class TMPFontMapping : ScriptableObject
    {
        [SerializeField] private TMPFontMappingEntry[] _entries;

        public TMP_FontAsset GetFont(LanguageCode language)
        {
            if (_entries == null) return null;

            foreach (TMPFontMappingEntry entry in _entries)
            {
                if (entry.Language == language) return entry.Font;
            }

            return null;
        }
    }

    [Serializable]
    public class TMPFontMappingEntry
    {
        [SerializeField] private LanguageCode _language;
        [SerializeField] private TMP_FontAsset _font;

        public LanguageCode Language => _language;
        public TMP_FontAsset Font => _font;
    }
}
