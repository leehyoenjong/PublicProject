using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 포인트 마일스톤 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class PointMilestone
    {
        [SerializeField] private int _requiredPoints;
        [SerializeField] private QuestReward[] _rewards;
        [SerializeField] private bool _isClaimed;

        public int RequiredPoints => _requiredPoints;
        public IReadOnlyList<QuestReward> Rewards => _rewards;
        public bool IsClaimed => _isClaimed;

        public void Claim()
        {
            _isClaimed = true;
        }
    }
}
