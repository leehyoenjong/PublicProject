using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 상세 UI
    /// </summary>
    public class QuestDetailUI : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Transform _conditionParent;
        [SerializeField] private GameObject _conditionEntryPrefab;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _abandonButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private IQuestSystem _questSystem;
        private string _currentQuestId;

        private void Start()
        {
            _questSystem = ServiceLocator.Get<IQuestSystem>();

            if (_acceptButton != null) _acceptButton.onClick.AddListener(OnAccept);
            if (_claimButton != null) _claimButton.onClick.AddListener(OnClaim);
            if (_abandonButton != null) _abandonButton.onClick.AddListener(OnAbandon);

            SetVisible(false);
        }

        public void Show(IQuestInstance quest)
        {
            _currentQuestId = quest.QuestId;

            if (_titleText != null) _titleText.text = quest.DisplayName;
            if (_descText != null) _descText.text = quest.Description;
            if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(quest.Progress * 100f)}%";

            UpdateConditions(quest);
            UpdateButtons(quest.State);
            SetVisible(true);
        }

        private void UpdateConditions(IQuestInstance quest)
        {
            if (_conditionParent == null || _conditionEntryPrefab == null) return;

            for (int i = _conditionParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_conditionParent.GetChild(i).gameObject);
            }

            IReadOnlyList<IConditionProgress> conditions = quest.GetConditions();
            foreach (IConditionProgress condition in conditions)
            {
                GameObject entry = Instantiate(_conditionEntryPrefab, _conditionParent);
                Text text = entry.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = $"{condition.Description}: {condition.CurrentAmount}/{condition.RequiredAmount}";
                }
            }
        }

        private void UpdateButtons(QuestState state)
        {
            if (_acceptButton != null) _acceptButton.gameObject.SetActive(state == QuestState.Available);
            if (_claimButton != null) _claimButton.gameObject.SetActive(state == QuestState.Completed);
            if (_abandonButton != null) _abandonButton.gameObject.SetActive(state == QuestState.InProgress);
        }

        private void OnAccept()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            _questSystem.AcceptQuest(_currentQuestId);
            RefreshCurrent();
        }

        private void OnClaim()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            _questSystem.ClaimReward(_currentQuestId);
            SetVisible(false);
        }

        private void OnAbandon()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            _questSystem.AbandonQuest(_currentQuestId);
            SetVisible(false);
        }

        private void RefreshCurrent()
        {
            if (string.IsNullOrEmpty(_currentQuestId)) return;
            IQuestInstance quest = _questSystem.GetProgress(_currentQuestId);
            if (quest != null) Show(quest);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
