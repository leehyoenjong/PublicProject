using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ICondition 구현체
    /// </summary>
    public class Condition : ICondition, IConditionProgress
    {
        private readonly ConditionData _data;
        private int _currentAmount;

        public string ConditionId => _data.ConditionId;
        public ConditionType ConditionType => _data.ConditionType;
        public string TargetId => _data.TargetId;
        public int RequiredAmount => _data.RequiredAmount;
        public int CurrentAmount => _currentAmount;
        public bool IsCompleted => _currentAmount >= _data.RequiredAmount;
        public float Progress => _data.RequiredAmount > 0 ? Mathf.Clamp01((float)_currentAmount / _data.RequiredAmount) : 1f;
        public string Description => _data.Description;

        public Condition(ConditionData data, int currentAmount = 0)
        {
            if (data == null)
            {
                Debug.LogError("[Condition] ConditionData is null");
            }
            _data = data;
            _currentAmount = currentAmount;
        }

        public void AddProgress(int amount)
        {
            if (IsCompleted) return;
            if (amount <= 0) return;

            _currentAmount = Mathf.Min(_currentAmount + amount, _data.RequiredAmount);
            Debug.Log($"[Condition] {_data.ConditionId}: {_currentAmount}/{_data.RequiredAmount}");
        }

        public void Reset()
        {
            _currentAmount = 0;
        }

        public void SetCurrentAmount(int amount)
        {
            _currentAmount = Mathf.Min(amount, _data.RequiredAmount);
        }
    }
}
