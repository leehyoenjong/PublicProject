using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IAchievementInstance 구현체
    /// </summary>
    public class AchievementInstance : IAchievementInstance
    {
        private readonly AchievementData _data;
        private int _currentAmount;
        private int _currentTier;
        private AchievementState _state;

        public string AchievementId => _data.AchievementId;
        public int DisplayName => _data.DisplayName;
        public int Description => _data.Description;
        public AchievementCategory Category => _data.Category;
        public AchievementState State => _state;
        public bool IsHidden => _data.IsHidden;
        public int CurrentTier => _currentTier;
        public int MaxTier => _data.Tiers != null ? _data.Tiers.Count : 0;
        public int CurrentAmount => _currentAmount;
        public AchievementData Data => _data;

        public int RequiredAmount
        {
            get
            {
                if (_data.Tiers == null || _currentTier >= _data.Tiers.Count) return 0;
                return _data.Tiers[_currentTier].RequiredAmount;
            }
        }

        public float Progress
        {
            get
            {
                int required = RequiredAmount;
                return required > 0 ? Mathf.Clamp01((float)_currentAmount / required) : 1f;
            }
        }

        public AchievementInstance(AchievementData data)
        {
            _data = data;
            _currentAmount = 0;
            _currentTier = 0;
            _state = AchievementState.InProgress;
        }

        public void AddProgress(int amount)
        {
            if (_state == AchievementState.Rewarded && _currentTier >= MaxTier) return;

            _currentAmount += amount;

            if (_currentAmount >= RequiredAmount && _state == AchievementState.InProgress)
            {
                _state = AchievementState.Completed;
                Debug.Log($"[도감] 티어 완료: {_data.AchievementId} (티어={_currentTier})");
            }
        }

        public bool ClaimCurrentTier()
        {
            if (_state != AchievementState.Completed) return false;

            _state = AchievementState.Rewarded;
            _currentTier++;

            if (_currentTier < MaxTier)
            {
                _state = AchievementState.InProgress;
            }

            return true;
        }

        public AchievementTierData GetCurrentTierData()
        {
            if (_data.Tiers == null || _currentTier >= _data.Tiers.Count) return null;
            return _data.Tiers[_currentTier];
        }

        public IReadOnlyList<AchievementTierData> GetTiers()
        {
            return _data.Tiers;
        }

        public void SetCurrentAmount(int amount)
        {
            _currentAmount = amount;
        }

        public void SetCurrentTier(int tier)
        {
            _currentTier = tier;
        }

        public void SetState(AchievementState state)
        {
            _state = state;
        }
    }
}
