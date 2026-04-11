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
        [SerializeField] private string _questId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private QuestType _questType;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _category;
        [SerializeField] private int _sortOrder;

        [Header("기간")]
        [SerializeField] private string _startDate;
        [SerializeField] private string _endDate;

        [Header("조건")]
        [SerializeField] private ConditionGroupType _conditionGroupType;
        [SerializeField] private ConditionData[] _conditions;

        [Header("보상")]
        [SerializeField] private QuestReward[] _rewards;

        [Header("선행 조건")]
        [SerializeField] private string[] _prerequisiteQuestIds;
        [SerializeField] private int _requiredLevel;

        [Header("자동")]
        [SerializeField] private bool _autoAccept;
        [SerializeField] private bool _autoComplete;

        public string QuestId => _questId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public QuestType QuestType => _questType;
        public ConditionGroupType ConditionGroupType => _conditionGroupType;
        public IReadOnlyList<ConditionData> Conditions => _conditions;
        public IReadOnlyList<QuestReward> Rewards => _rewards;
        public IReadOnlyList<string> PrerequisiteQuestIds => _prerequisiteQuestIds;
        public int RequiredLevel => _requiredLevel;
        public bool AutoAccept => _autoAccept;
        public bool AutoComplete => _autoComplete;
        public Sprite Icon => _icon;
        public string Category => _category;
        public int SortOrder => _sortOrder;
        public string StartDate => _startDate;
        public string EndDate => _endDate;
    }
}
