using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 우편 보상 항목. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class MailRewardEntry
    {
        [SerializeField] private string _rewardId;
        [SerializeField] private RewardType _rewardType;
        [SerializeField] private int _amount;

        public string RewardId { get => _rewardId; set => _rewardId = value; }
        public RewardType RewardType { get => _rewardType; set => _rewardType = value; }
        public int Amount { get => _amount; set => _amount = value; }
    }
}
