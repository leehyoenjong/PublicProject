using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 복합 조건 (All/Any/Sequence)
    /// </summary>
    public class ConditionGroup
    {
        private readonly ConditionGroupType _groupType;
        private readonly List<ICondition> _conditions = new List<ICondition>();

        public ConditionGroupType GroupType => _groupType;
        public IReadOnlyList<ICondition> Conditions => _conditions.AsReadOnly();

        public ConditionGroup(ConditionGroupType groupType)
        {
            _groupType = groupType;
        }

        public void AddCondition(ICondition condition)
        {
            if (condition == null)
            {
                Debug.LogWarning("[ConditionGroup] Null condition ignored");
                return;
            }
            _conditions.Add(condition);
        }

        public bool IsCompleted
        {
            get
            {
                if (_conditions.Count == 0) return true;

                switch (_groupType)
                {
                    case ConditionGroupType.All:
                        foreach (ICondition c in _conditions)
                        {
                            if (!c.IsCompleted) return false;
                        }
                        return true;

                    case ConditionGroupType.Any:
                        foreach (ICondition c in _conditions)
                        {
                            if (c.IsCompleted) return true;
                        }
                        return false;

                    case ConditionGroupType.Sequence:
                        foreach (ICondition c in _conditions)
                        {
                            if (!c.IsCompleted) return false;
                        }
                        return true;

                    default:
                        return false;
                }
            }
        }

        public float Progress
        {
            get
            {
                if (_conditions.Count == 0) return 1f;

                float total = 0f;
                foreach (ICondition c in _conditions)
                {
                    total += c.Progress;
                }
                return total / _conditions.Count;
            }
        }

        public ICondition GetActiveCondition()
        {
            if (_groupType != ConditionGroupType.Sequence) return null;

            foreach (ICondition c in _conditions)
            {
                if (!c.IsCompleted) return c;
            }
            return null;
        }

        public void ResetAll()
        {
            foreach (ICondition c in _conditions)
            {
                c.Reset();
            }
        }
    }
}
