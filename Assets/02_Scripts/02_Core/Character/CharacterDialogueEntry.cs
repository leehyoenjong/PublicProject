using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// CharacterInfo 에 ChildTable 로 주입되는 이벤트별 대사.
    /// 시트 CharacterDialogue 의 parentId/order 는 매칭·정렬 예약어라 필드에 매핑하지 않는다.
    /// lineKey=0 이면 UI 에서 스킵.
    /// </summary>
    [System.Serializable]
    public class CharacterDialogueEntry
    {
        [SerializeField, SheetAlias("event")] private DialogueEvent _event;
        [SerializeField, LocalizationKey, SheetAlias("lineKey")] private int _lineKey;

        public DialogueEvent Event => _event;
        public int LineKey => _lineKey;
        public bool HasLine => _lineKey > 0;
    }
}
