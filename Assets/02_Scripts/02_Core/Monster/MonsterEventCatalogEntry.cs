using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// MonsterEvent 시트 한 행. eventId 카탈로그로 활용 — 시트의 onSpawnEvents/onDeathEvents 와 매칭.
    /// 코드는 카탈로그 등록 여부 검증·도구화 용도, 실제 핸들러는 EventSystem 측에서 처리.
    /// </summary>
    [System.Serializable]
    public class MonsterEventCatalogEntry
    {
        [SerializeField, SheetAlias("eventId")] private string _eventId;
        [SerializeField, SheetAlias("kind")] private MonsterEventKind _kind;
        [SerializeField, LocalizationKey, SheetAlias("descKey")] private int _descKey;

        public string EventId => _eventId;
        public MonsterEventKind Kind => _kind;
        public int DescKey => _descKey;
    }
}
