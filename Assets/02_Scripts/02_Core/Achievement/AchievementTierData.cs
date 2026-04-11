using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 업적 단계별 목표/보상/포인트/칭호. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class AchievementTierData
    {
        [SerializeField] private int _requiredAmount;
        [SerializeField] private QuestReward[] _rewards;
        [SerializeField] private int _points;
        [SerializeField] private string _title;

        public int RequiredAmount => _requiredAmount;
        public IReadOnlyList<QuestReward> Rewards => _rewards;
        public int Points => _points;
        public string Title => _title;
    }
}
