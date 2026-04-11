using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 업적 목록 UI
    /// </summary>
    public class AchievementListUI : MonoBehaviour
    {
        [SerializeField] private Transform _slotParent;
        [SerializeField] private AchievementSlotUI _slotPrefab;
        [SerializeField] private AchievementDetailUI _detailUI;
        [SerializeField] private Text _totalPointsText;

        private IAchievementSystem _achievementSystem;
        private IEventBus _eventBus;
        private AchievementCategory? _currentFilter;
        private readonly List<AchievementSlotUI> _slots = new List<AchievementSlotUI>();

        private void Start()
        {
            _achievementSystem = ServiceLocator.Get<IAchievementSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<AchievementCompletedEvent>(OnAchievementCompleted);
            _eventBus.Subscribe<AchievementRewardClaimedEvent>(OnRewardClaimed);

            RefreshList();
            Debug.Log("[AchievementListUI] Init started");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<AchievementCompletedEvent>(OnAchievementCompleted);
            _eventBus.Unsubscribe<AchievementRewardClaimedEvent>(OnRewardClaimed);
        }

        public void SetFilter(int categoryIndex)
        {
            _currentFilter = categoryIndex >= 0 ? (AchievementCategory)categoryIndex : null;
            RefreshList();
        }

        public void RefreshList()
        {
            ClearSlots();

            IReadOnlyList<IAchievementInstance> achievements = _achievementSystem.GetAchievements(_currentFilter);

            if (_totalPointsText != null)
            {
                _totalPointsText.text = _achievementSystem.GetTotalPoints().ToString();
            }

            if (_slotPrefab == null || _slotParent == null) return;

            foreach (IAchievementInstance achievement in achievements)
            {
                if (achievement.IsHidden && achievement.State == AchievementState.InProgress && achievement.Progress <= 0f)
                {
                    continue;
                }

                AchievementSlotUI slot = Instantiate(_slotPrefab, _slotParent);
                slot.SetData(achievement, () => OnSlotClicked(achievement.AchievementId));
                _slots.Add(slot);
            }
        }

        private void OnSlotClicked(string achievementId)
        {
            if (_detailUI != null)
            {
                IReadOnlyList<IAchievementInstance> all = _achievementSystem.GetAchievements();
                foreach (IAchievementInstance a in all)
                {
                    if (a.AchievementId == achievementId)
                    {
                        _detailUI.Show(a);
                        break;
                    }
                }
            }
        }

        private void OnAchievementCompleted(AchievementCompletedEvent evt) => RefreshList();
        private void OnRewardClaimed(AchievementRewardClaimedEvent evt) => RefreshList();

        private void ClearSlots()
        {
            foreach (AchievementSlotUI slot in _slots)
            {
                Destroy(slot.gameObject);
            }
            _slots.Clear();
        }
    }
}
