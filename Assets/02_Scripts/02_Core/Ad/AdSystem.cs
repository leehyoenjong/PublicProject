using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IAdSystem 구현체.
    /// 일일 제한, 쿨타임, VIP 면제 체크.
    /// </summary>
    public class AdSystem : IAdSystem
    {
        private readonly IAdAdapter _adAdapter;
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly IAPConfig _config;

        private readonly Dictionary<string, int> _dailyWatchCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _lastWatchTime = new Dictionary<string, float>();

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_DAILY = "ad_daily_data";
        private const string SAVE_KEY_DATE = "ad_daily_date";

        private string _lastSavedDate;
        private bool _isVIP;

        public AdSystem(IAdAdapter adAdapter, IEventBus eventBus, ISaveSystem saveSystem, IAPConfig config)
        {
            _adAdapter = adAdapter;
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _config = config;

            LoadDailyData();

            Debug.Log("[AdSystem] Init started");
        }

        public void ShowAd(string slotId, Action onSuccess, Action<AdFailReason> onFail)
        {
            AdSlotData slot = _config.GetAdSlot(slotId);
            if (slot == null)
            {
                Debug.LogError($"[AdSystem] Slot not found: {slotId}");
                onFail?.Invoke(AdFailReason.AdapterError);
                return;
            }

            if (!CanShowAd(slotId))
            {
                AdFailReason reason = GetBlockReason(slotId, slot);
                onFail?.Invoke(reason);
                return;
            }

            _eventBus?.Publish(new AdStartEvent
            {
                SlotId = slotId,
                AdType = slot.AdType
            });

            Debug.Log($"[AdSystem] Showing ad: {slotId} ({slot.AdType})");

            _adAdapter.ShowAd(slot.AdType, slotId,
                () =>
                {
                    RecordWatch(slotId);
                    GrantRewards(slot);

                    _eventBus?.Publish(new AdCompleteEvent
                    {
                        SlotId = slotId,
                        AdType = slot.AdType,
                        Rewarded = slot.AdType == AdType.Rewarded
                    });

                    Debug.Log($"[AdSystem] Ad complete: {slotId}");
                    onSuccess?.Invoke();
                },
                reason =>
                {
                    _eventBus?.Publish(new AdFailEvent
                    {
                        SlotId = slotId,
                        AdType = slot.AdType,
                        Reason = reason
                    });

                    Debug.Log($"[AdSystem] Ad failed: {slotId} ({reason})");
                    onFail?.Invoke(reason);
                });
        }

        public bool CanShowAd(string slotId)
        {
            AdSlotData slot = _config.GetAdSlot(slotId);
            if (slot == null) return false;

            if (_isVIP && slot.AdType != AdType.Rewarded) return false;

            CheckDateReset();

            int watchCount = GetDailyWatchCount(slotId);
            if (slot.DailyLimit > 0 && watchCount >= slot.DailyLimit) return false;

            if (slot.CooldownSeconds > 0f && _lastWatchTime.TryGetValue(slotId, out float lastTime))
            {
                if (Time.realtimeSinceStartup - lastTime < slot.CooldownSeconds) return false;
            }

            return true;
        }

        public int GetDailyWatchCount(string slotId)
        {
            CheckDateReset();
            return _dailyWatchCounts.TryGetValue(slotId, out int count) ? count : 0;
        }

        /// <summary>
        /// 남은 광고 시청 횟수. DailyLimit이 0 이하이면 -1(무제한) 반환.
        /// </summary>
        public int GetRemainingWatches(string slotId)
        {
            AdSlotData slot = _config.GetAdSlot(slotId);
            if (slot == null) return 0;
            if (slot.DailyLimit <= 0) return -1;

            int watched = GetDailyWatchCount(slotId);
            return Mathf.Max(0, slot.DailyLimit - watched);
        }

        public void ShowBanner(BannerPosition position)
        {
            if (_isVIP)
            {
                Debug.Log("[AdSystem] VIP — banner skipped");
                return;
            }

            _adAdapter.ShowBanner(position);
            Debug.Log($"[AdSystem] Banner shown: {position}");
        }

        public void HideBanner()
        {
            _adAdapter.HideBanner();
            Debug.Log("[AdSystem] Banner hidden");
        }

        public void SetVIP(bool isVIP)
        {
            _isVIP = isVIP;

            if (_isVIP)
            {
                HideBanner();
            }

            Debug.Log($"[AdSystem] VIP state: {_isVIP}");
        }

        private void RecordWatch(string slotId)
        {
            if (!_dailyWatchCounts.ContainsKey(slotId))
            {
                _dailyWatchCounts[slotId] = 0;
            }
            _dailyWatchCounts[slotId]++;
            _lastWatchTime[slotId] = Time.realtimeSinceStartup;

            SaveDailyData();
        }

        private void GrantRewards(AdSlotData slot)
        {
            if (slot.Rewards == null) return;

            foreach (IAPRewardEntry reward in slot.Rewards)
            {
                _eventBus?.Publish(new RewardGrantedEvent
                {
                    ProductId = slot.SlotId,
                    RewardId = reward.RewardId,
                    RewardType = reward.RewardType,
                    Amount = reward.Amount,
                    Source = "Ad"
                });

                Debug.Log($"[AdSystem] Reward granted: {reward.RewardId} x{reward.Amount}");
            }
        }

        private AdFailReason GetBlockReason(string slotId, AdSlotData slot)
        {
            if (_isVIP && slot.AdType != AdType.Rewarded) return AdFailReason.VIPExempt;

            int watchCount = GetDailyWatchCount(slotId);
            if (slot.DailyLimit > 0 && watchCount >= slot.DailyLimit)
            {
                return AdFailReason.DailyLimitReached;
            }

            if (slot.CooldownSeconds > 0f && _lastWatchTime.TryGetValue(slotId, out float lastTime))
            {
                if (Time.realtimeSinceStartup - lastTime < slot.CooldownSeconds)
                {
                    return AdFailReason.CooldownActive;
                }
            }

            return AdFailReason.NotLoaded;
        }

        private void CheckDateReset()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_lastSavedDate != today)
            {
                _dailyWatchCounts.Clear();
                _lastSavedDate = today;
                SaveDailyData();
                Debug.Log("[AdSystem] Daily data reset");
            }
        }

        private void LoadDailyData()
        {
            if (_saveSystem == null) return;

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_DATE))
            {
                _lastSavedDate = _saveSystem.Load<string>(SAVE_SLOT, SAVE_KEY_DATE);
            }

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_lastSavedDate != today)
            {
                _lastSavedDate = today;
                return;
            }

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_DAILY))
            {
                var data = _saveSystem.Load<Dictionary<string, int>>(SAVE_SLOT, SAVE_KEY_DAILY);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _dailyWatchCounts[kvp.Key] = kvp.Value;
                    }
                }
            }

            Debug.Log("[AdSystem] Daily data loaded");
        }

        private void SaveDailyData()
        {
            if (_saveSystem == null) return;

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_DATE, _lastSavedDate);
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_DAILY, _dailyWatchCounts);
            _saveSystem.WriteToDisk(SAVE_SLOT);
        }
    }
}
