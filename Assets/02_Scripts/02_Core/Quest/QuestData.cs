using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 정의 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestData", menuName = "PublicFramework/Quest/QuestData")]
    public class QuestData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _questId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("type")] private QuestType _questType;
        [SerializeField] private string _category;
        [SerializeField, SheetAlias("order")] private int _sortOrder;

        [Header("기간")]
        [SerializeField] private string _startDate;
        [SerializeField] private string _endDate;

        [Header("조건")]
        [SerializeField, SheetAlias("groupType")] private ConditionGroupType _conditionGroupType;
        [SerializeField] private ConditionData[] _conditions;

        [Header("보상")]
        [SerializeField] private QuestReward[] _rewards;

        [Header("선행 조건")]
        [SerializeField, SheetAlias("prereqIds")] private string[] _prerequisiteQuestIds;
        [SerializeField, SheetAlias("reqLevel")] private int _requiredLevel;

        [Header("자동")]
        [SerializeField] private bool _autoComplete;

        public string QuestId => _questId;
        public int DisplayName => _displayName;
        public int Description => _description;
        public QuestType QuestType => _questType;
        public ConditionGroupType ConditionGroupType => _conditionGroupType;
        public IReadOnlyList<ConditionData> Conditions => _conditions;
        public IReadOnlyList<QuestReward> Rewards => _rewards;
        public IReadOnlyList<string> PrerequisiteQuestIds => _prerequisiteQuestIds;
        public int RequiredLevel => _requiredLevel;
        public bool AutoComplete => _autoComplete;
        public string Category => _category;
        public int SortOrder => _sortOrder;
        public string StartDate => _startDate;
        public string EndDate => _endDate;
    }
}
