using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IAchievementSystem 구현체. SaveSystem 연동.
    /// 보상: 아이템(QuestReward) + 영구 보유효과(PassiveStat).
    /// </summary>
    public class AchievementSystem : IAchievementSystem
    {
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<string, AchievementInstance> _achievements = new Dictionary<string, AchievementInstance>();
        private IRewardHandler _rewardHandler;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_ACHIEVEMENTS = "achievement_data";

        public AchievementSystem(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            Debug.Log("[AchievementSystem] Init started");
        }

        /// <summary>
        /// 업적 등록 후 호출하여 저장된 진행도를 복원한다.
        /// </summary>
        public void Initialize()
        {
            LoadData();
            Debug.Log("[AchievementSystem] Initialized — achievement data loaded");
        }

        public void SetRewardHandler(IRewardHandler handler)
        {
            _rewardHandler = handler;
        }

        public void RegisterAchievement(AchievementData data)
        {
            if (data == null || _achievements.ContainsKey(data.AchievementId)) return;

            var instance = new AchievementInstance(data);
            _achievements[data.AchievementId] = instance;
        }

        public void NotifyProgress(ConditionType type, string targetId, int amount)
        {
            foreach (AchievementInstance achievement in _achievements.Values)
            {
                if (achievement.State == AchievementState.Rewarded && achievement.CurrentTier >= achievement.MaxTier) continue;
                if (achievement.Data.ConditionType != type) continue;
                if (!string.IsNullOrEmpty(achievement.Data.ConditionTargetId) && achievement.Data.ConditionTargetId != targetId) continue;

                bool wasCompleted = achievement.State == AchievementState.Completed;
                achievement.AddProgress(amount);

                _eventBus?.Publish(new AchievementProgressEvent
                {
                    AchievementId = achievement.AchievementId,
                    CurrentAmount = achievement.CurrentAmount,
                    RequiredAmount = achievement.RequiredAmount
                });

                if (!wasCompleted && achievement.State == AchievementState.Completed)
                {
                    _eventBus?.Publish(new AchievementCompletedEvent
                    {
                        AchievementId = achievement.AchievementId,
                        Category = achievement.Category,
                        Tier = achievement.CurrentTier
                    });
                }
            }

            SaveData();
        }

        public bool ClaimReward(string achievementId)
        {
            if (!_achievements.TryGetValue(achievementId, out AchievementInstance achievement)) return false;
            if (achievement.State != AchievementState.Completed) return false;

            AchievementTierData tierData = achievement.GetCurrentTierData();
            if (tierData == null) return false;

            int claimedTierIndex = achievement.CurrentTier;
            achievement.ClaimCurrentTier();

            if (tierData.Rewards != null)
            {
                foreach (QuestReward reward in tierData.Rewards)
                {
                    _rewardHandler?.HandleReward(reward.RewardId, reward.Amount, "Achievement");
                }
            }

            _eventBus?.Publish(new AchievementRewardClaimedEvent
            {
                AchievementId = achievementId,
                Tier = claimedTierIndex
            });

            SaveData();
            Debug.Log($"[AchievementSystem] Reward claimed: {achievementId} tier {claimedTierIndex}");
            return true;
        }

        public AchievementState GetState(string achievementId)
        {
            return _achievements.TryGetValue(achievementId, out AchievementInstance a) ? a.State : AchievementState.Locked;
        }

        public float GetProgress(string achievementId)
        {
            return _achievements.TryGetValue(achievementId, out AchievementInstance a) ? a.Progress : 0f;
        }

        public IReadOnlyList<IAchievementInstance> GetAchievements(AchievementCategory? category = null)
        {
            var result = new List<IAchievementInstance>();

            foreach (AchievementInstance achievement in _achievements.Values)
            {
                if (category.HasValue && achievement.Category != category.Value) continue;
                result.Add(achievement);
            }

            return result.AsReadOnly();
        }

        public IReadOnlyList<PassiveStat> GetActivePassiveStats()
        {
            var result = new List<PassiveStat>();

            foreach (AchievementInstance achievement in _achievements.Values)
            {
                IReadOnlyList<AchievementTierData> tiers = achievement.Data.Tiers;
                if (tiers == null) continue;

                int claimedCount = achievement.CurrentTier;
                if (achievement.State == AchievementState.Rewarded && claimedCount < achievement.MaxTier)
                {
                    // Rewarded 상태에서 다음 티어로 넘어간 경우 이미 CurrentTier 가 증가된 상태
                }

                for (int i = 0; i < claimedCount && i < tiers.Count; i++)
                {
                    IReadOnlyList<PassiveStat> stats = tiers[i].PassiveStats;
                    if (stats == null) continue;
                    foreach (PassiveStat s in stats)
                    {
                        if (s != null) result.Add(s);
                    }
                }
            }

            return result.AsReadOnly();
        }

        private void SaveData()
        {
            if (_saveSystem == null) return;

            var data = new Dictionary<string, int[]>();
            foreach (var kvp in _achievements)
            {
                data[kvp.Key] = new int[] { kvp.Value.CurrentAmount, kvp.Value.CurrentTier, (int)kvp.Value.State };
            }

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_ACHIEVEMENTS, data);
        }

        private void LoadData()
        {
            if (_saveSystem == null) return;

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_ACHIEVEMENTS))
            {
                var data = _saveSystem.Load<Dictionary<string, int[]>>(SAVE_SLOT, SAVE_KEY_ACHIEVEMENTS);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        if (!_achievements.TryGetValue(kvp.Key, out AchievementInstance achievement)) continue;

                        int[] values = kvp.Value;
                        if (values.Length >= 3)
                        {
                            achievement.SetCurrentAmount(values[0]);
                            achievement.SetCurrentTier(values[1]);
                            achievement.SetState((AchievementState)values[2]);
                        }
                    }
                }
            }

            Debug.Log($"[AchievementSystem] Loaded {_achievements.Count} achievements");
        }
    }
}
