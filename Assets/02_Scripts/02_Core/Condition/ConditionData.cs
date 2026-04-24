using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 조건 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class ConditionData
    {
        [SerializeField] private string _conditionId;
        [SerializeField, SheetAlias("type")] private ConditionType _conditionType;
        [SerializeField] private string _targetId;
        [SerializeField, SheetAlias("amount")] private int _requiredAmount;
        [SerializeField] private string _description;

        public string ConditionId => _conditionId;
        public ConditionType ConditionType => _conditionType;
        public string TargetId => _targetId;
        public int RequiredAmount => _requiredAmount;
        public string Description => _description;
    }
}
