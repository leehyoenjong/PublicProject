using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트/업적 보상 항목. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        [SerializeField] private string _rewardId;
        [SerializeField] private RewardType _rewardType;
        [SerializeField] private int _amount;

        public string RewardId => _rewardId;
        public RewardType RewardType => _rewardType;
        public int Amount => _amount;
    }
}
