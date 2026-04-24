using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 업적 단계별 목표/보상/보유효과. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class AchievementTierData
    {
        [SerializeField, SheetAlias("amount")] private int _requiredAmount;
        [SerializeField] private QuestReward[] _rewards;
        [SerializeField] private PassiveStat[] _passiveStats;

        public int RequiredAmount => _requiredAmount;
        public IReadOnlyList<QuestReward> Rewards => _rewards;
        public IReadOnlyList<PassiveStat> PassiveStats => _passiveStats;
    }
}
