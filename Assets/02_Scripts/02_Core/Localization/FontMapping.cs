using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 언어별 폰트 매핑 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "FontMapping", menuName = "PublicFramework/Localization/FontMapping")]
    public class FontMapping : ScriptableObject
    {
        [SerializeField] private FontMappingEntry[] _entries;

        public Font GetFont(LanguageCode language)
        {
            if (_entries == null) return null;

            foreach (FontMappingEntry entry in _entries)
            {
                if (entry.Language == language) return entry.Font;
            }

            return null;
        }
    }

    [Serializable]
    public class FontMappingEntry
    {
        [SerializeField] private LanguageCode _language;
        [SerializeField] private Font _font;

        public LanguageCode Language => _language;
        public Font Font => _font;
    }
}
