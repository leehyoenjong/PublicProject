using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트/업적 보상 항목. itemId 는 ItemData MID(int), 타입 정보는 ItemData 가 관리.
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        [SerializeField] private int _rewardId;
        [SerializeField] private int _amount;

        public int RewardId => _rewardId;
        public int Amount => _amount;

        public QuestReward() { }

        public QuestReward(int rewardId, int amount)
        {
            _rewardId = rewardId;
            _amount = amount;
        }
    }
}
