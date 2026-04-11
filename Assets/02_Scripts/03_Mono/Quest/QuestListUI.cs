using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 목록 UI
    /// </summary>
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private Transform _slotParent;
        [SerializeField] private QuestSlotUI _slotPrefab;
        [SerializeField] private QuestDetailUI _detailUI;
        [SerializeField] private Button[] _tabButtons;

        private IQuestSystem _questSystem;
        private IEventBus _eventBus;
        private QuestType? _currentFilter;
        private readonly List<QuestSlotUI> _slots = new List<QuestSlotUI>();

        private void Start()
        {
            _questSystem = ServiceLocator.Get<IQuestSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
            _eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Subscribe<QuestRewardClaimedEvent>(OnQuestRewarded);
            _eventBus.Subscribe<QuestUnlockedEvent>(OnQuestUnlocked);

            RefreshList();
            Debug.Log("[QuestListUI] Init started");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestChanged);
            _eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Unsubscribe<QuestRewardClaimedEvent>(OnQuestRewarded);
            _eventBus.Unsubscribe<QuestUnlockedEvent>(OnQuestUnlocked);
        }

        public void SetFilter(int typeIndex)
        {
            _currentFilter = typeIndex >= 0 ? (QuestType)typeIndex : null;
            RefreshList();
        }

        public void RefreshList()
        {
            ClearSlots();

            IReadOnlyList<IQuestInstance> quests = _questSystem.GetQuests(typeFilter: _currentFilter);

            if (_slotPrefab == null || _slotParent == null) return;

            foreach (IQuestInstance quest in quests)
            {
                if (quest.State == QuestState.Locked) continue;

                QuestSlotUI slot = Instantiate(_slotPrefab, _slotParent);
                slot.SetData(quest, () => OnSlotClicked(quest.QuestId));
                _slots.Add(slot);
            }
        }

        private void OnSlotClicked(string questId)
        {
            if (_detailUI != null)
            {
                IQuestInstance quest = _questSystem.GetProgress(questId);
                if (quest != null) _detailUI.Show(quest);
            }
        }

        private void OnQuestChanged(QuestAcceptedEvent evt) => RefreshList();
        private void OnQuestCompleted(QuestCompletedEvent evt) => RefreshList();
        private void OnQuestRewarded(QuestRewardClaimedEvent evt) => RefreshList();
        private void OnQuestUnlocked(QuestUnlockedEvent evt) => RefreshList();

        private void ClearSlots()
        {
            foreach (QuestSlotUI slot in _slots)
            {
                Destroy(slot.gameObject);
            }
            _slots.Clear();
        }
    }
}
