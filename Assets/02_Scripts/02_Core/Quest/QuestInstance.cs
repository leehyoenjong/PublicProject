using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IQuestInstance 구현체
    /// </summary>
    public class QuestInstance : IQuestInstance
    {
        private readonly QuestData _data;
        private readonly ConditionGroup _conditionGroup;
        private QuestState _state;

        public string QuestId => _data.QuestId;
        public QuestType QuestType => _data.QuestType;
        public QuestState State => _state;
        public string DisplayName => _data.DisplayName;
        public string Description => _data.Description;
        public float Progress => _conditionGroup.Progress;
        public bool IsCompleted => _conditionGroup.IsCompleted;

        public QuestData Data => _data;
        public ConditionGroup ConditionGroup => _conditionGroup;

        public QuestInstance(QuestData data)
        {
            _data = data;
            _state = QuestState.Locked;
            _conditionGroup = new ConditionGroup(data.ConditionGroupType);

            foreach (ConditionData cd in data.Conditions)
            {
                _conditionGroup.AddCondition(new Condition(cd));
            }
        }

        public void SetState(QuestState state)
        {
            _state = state;
        }

        public IReadOnlyList<IConditionProgress> GetConditions()
        {
            var list = new List<IConditionProgress>();
            foreach (ICondition c in _conditionGroup.Conditions)
            {
                if (c is IConditionProgress progress)
                {
                    list.Add(progress);
                }
            }
            return list.AsReadOnly();
        }

        public IReadOnlyList<QuestReward> GetRewards()
        {
            return _data.Rewards;
        }

        public void ResetConditions()
        {
            _conditionGroup.ResetAll();
        }
    }
}
