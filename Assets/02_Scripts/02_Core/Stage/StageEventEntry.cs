using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 이벤트 한 건. StageData._events 자식 시트로 주입.
    /// 보물찾기/펫구출/튜토리얼/컷씬/아이템지급/다이얼로그/커스텀 7종.
    /// </summary>
    [Serializable]
    public class StageEventEntry
    {
        [SerializeField] private StageEventType _eventType;
        [SerializeField] private string _targetId;
        [SerializeField] private StageEventTrigger _triggerType;
        [SerializeField] private string _triggerValue;
        [SerializeField] private QuestReward[] _rewardItems;
        [SerializeField, LocalizationKey] private int _description;
        [SerializeField] private bool _canRepeat;

        public StageEventType EventType => _eventType;
        public string TargetId => _targetId;
        public StageEventTrigger TriggerType => _triggerType;
        public string TriggerValue => _triggerValue;
        public QuestReward[] RewardItems => _rewardItems;
        public int Description => _description;
        public bool CanRepeat => _canRepeat;

        public StageEventEntry() { }

        public StageEventEntry(StageEventType eventType, string targetId,
            StageEventTrigger triggerType, string triggerValue,
            QuestReward[] rewardItems, int description, bool canRepeat)
        {
            _eventType = eventType;
            _targetId = targetId;
            _triggerType = triggerType;
            _triggerValue = triggerValue;
            _rewardItems = rewardItems;
            _description = description;
            _canRepeat = canRepeat;
        }
    }
}
