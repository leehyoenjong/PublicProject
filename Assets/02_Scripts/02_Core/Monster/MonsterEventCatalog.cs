using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// MonsterEvent 시트 전체를 담는 SO (단일 카탈로그). MonsterSystem 이 EventId 검증/조회에 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterEventCatalog", menuName = "PublicFramework/Monster/MonsterEvent Catalog")]
    public class MonsterEventCatalog : ScriptableObject
    {
        [SerializeField] private MonsterEventCatalogEntry[] _entries;

        public IReadOnlyList<MonsterEventCatalogEntry> Entries =>
            _entries ?? System.Array.Empty<MonsterEventCatalogEntry>();

        public MonsterEventCatalogEntry GetEntry(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || _entries == null) return null;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i] != null && _entries[i].EventId == eventId) return _entries[i];
            }
            return null;
        }
    }
}
