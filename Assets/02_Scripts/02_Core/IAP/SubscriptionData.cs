using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 구독 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class SubscriptionData
    {
        [SerializeField] private SubscriptionPeriod _period;
        [SerializeField] private int _durationDays;
        [SerializeField] private IAPRewardEntry[] _dailyRewards;
        [SerializeField] private IAPRewardEntry[] _immediateRewards;
        [SerializeField] private bool _autoRenew;

        public SubscriptionPeriod Period { get => _period; set => _period = value; }
        public int DurationDays { get => _durationDays; set => _durationDays = value; }
        public IReadOnlyList<IAPRewardEntry> DailyRewards => _dailyRewards;
        public void SetDailyRewards(IAPRewardEntry[] rewards) { _dailyRewards = rewards; }
        public IReadOnlyList<IAPRewardEntry> ImmediateRewards => _immediateRewards;
        public void SetImmediateRewards(IAPRewardEntry[] rewards) { _immediateRewards = rewards; }
        public bool AutoRenew { get => _autoRenew; set => _autoRenew = value; }
    }
}
