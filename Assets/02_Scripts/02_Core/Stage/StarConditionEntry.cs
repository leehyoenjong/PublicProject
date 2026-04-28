using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 별점 조건 1단계. StageData._starConditions 인라인 배열 (3성/2성/1성 순).
    /// ConditionType 재사용 (Quest/Achievement 와 동일).
    /// </summary>
    [Serializable]
    public class StarConditionEntry
    {
        [SerializeField] private ConditionType _conditionType;
        [SerializeField] private int _amount;

        public ConditionType ConditionType => _conditionType;
        public int Amount => _amount;

        public StarConditionEntry() { }

        public StarConditionEntry(ConditionType conditionType, int amount)
        {
            _conditionType = conditionType;
            _amount = amount;
        }
    }
}
