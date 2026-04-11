using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IAchievementSystem 구현체. SaveSystem 연동, 포인트 마일스톤.
    /// </summary>
    public class AchievementSystem : IAchievementSystem
    {
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<string, AchievementInstance> _achievements = new Dictionary<string, AchievementInstance>();
        private readonly List<PointMilestone> _milestones = new List<PointMilestone>();
        private IRewardHandler _rewardHandler;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_ACHIEVEMENTS = "achievement_data";
        private const string SAVE_KEY_MILESTONES = "achievement_milestones";

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

        public void AddMilestone(PointMilestone milestone)
        {
            _milestones.Add(milestone);
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

            achievement.ClaimCurrentTier();

            if (tierData.Rewards != null)
            {
                foreach (QuestReward reward in tierData.Rewards)
                {
                    _rewardHandler?.HandleReward(reward.RewardId, reward.RewardType, reward.Amount, "Achievement");
                }
            }

            _eventBus?.Publish(new AchievementRewardClaimedEvent
            {
                AchievementId = achievementId,
                Tier = achievement.CurrentTier - 1,
                Points = tierData.Points
            });

            if (!string.IsNullOrEmpty(tierData.Title))
            {
                _eventBus?.Publish(new AchievementTitleUnlockedEvent
                {
                    AchievementId = achievementId,
                    Title = tierData.Title
                });
            }

            _eventBus?.Publish(new AchievementPointsChangedEvent
            {
                TotalPoints = GetTotalPoints(),
                AddedPoints = tierData.Points
            });

            SaveData();
            Debug.Log($"[AchievementSystem] Reward claimed: {achievementId} tier {achievement.CurrentTier - 1}");
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

        public int GetTotalPoints()
        {
            int total = 0;
            foreach (AchievementInstance achievement in _achievements.Values)
            {
                total += achievement.TotalPoints;
            }
            return total;
        }

        public IReadOnlyList<PointMilestone> GetPointMilestones()
        {
            return _milestones.AsReadOnly();
        }

        public bool ClaimMilestone(int milestoneIndex)
        {
            if (milestoneIndex < 0 || milestoneIndex >= _milestones.Count) return false;

            PointMilestone milestone = _milestones[milestoneIndex];
            if (milestone.IsClaimed) return false;
            if (GetTotalPoints() < milestone.RequiredPoints) return false;

            milestone.Claim();

            if (milestone.Rewards != null)
            {
                foreach (QuestReward reward in milestone.Rewards)
                {
                    _rewardHandler?.HandleReward(reward.RewardId, reward.RewardType, reward.Amount, "Milestone");
                }
            }

            _eventBus?.Publish(new AchievementMilestoneClaimedEvent
            {
                MilestoneIndex = milestoneIndex,
                RequiredPoints = milestone.RequiredPoints
            });

            SaveData();
            Debug.Log($"[AchievementSystem] Milestone claimed: {milestoneIndex} ({milestone.RequiredPoints} pts)");
            return true;
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

            // 마일스톤 수령 상태 저장
            var milestoneStates = new bool[_milestones.Count];
            for (int i = 0; i < _milestones.Count; i++)
            {
                milestoneStates[i] = _milestones[i].IsClaimed;
            }
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_MILESTONES, milestoneStates);
        }

        private void LoadData()
        {
            if (_saveSystem == null) return;

            // 업적 진행도 복원
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

            // 마일스톤 수령 상태 복원
            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_MILESTONES))
            {
                var milestoneStates = _saveSystem.Load<bool[]>(SAVE_SLOT, SAVE_KEY_MILESTONES);
                if (milestoneStates != null)
                {
                    for (int i = 0; i < _milestones.Count && i < milestoneStates.Length; i++)
                    {
                        if (milestoneStates[i])
                        {
                            _milestones[i].Claim();
                        }
                    }
                }
            }

            Debug.Log($"[AchievementSystem] Loaded {_achievements.Count} achievements");
        }
    }
}
