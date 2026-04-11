using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 광고 슬롯 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class AdSlotData
    {
        [SerializeField] private string _slotId;
        [SerializeField] private AdType _adType;
        [SerializeField] private IAPRewardEntry[] _rewards;
        [SerializeField] private int _dailyLimit;
        [SerializeField] private float _cooldownSeconds;

        public string SlotId { get => _slotId; set => _slotId = value; }
        public AdType AdType { get => _adType; set => _adType = value; }
        public IReadOnlyList<IAPRewardEntry> Rewards => _rewards;
        public void SetRewards(IAPRewardEntry[] rewards) { _rewards = rewards; }
        public int DailyLimit { get => _dailyLimit; set => _dailyLimit = value; }
        public float CooldownSeconds { get => _cooldownSeconds; set => _cooldownSeconds = value; }
    }
}
