using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 업적 상세 UI
    /// </summary>
    public class AchievementDetailUI : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Text _tierText;
        [SerializeField] private Transform _tierListParent;
        [SerializeField] private GameObject _tierEntryPrefab;
        [SerializeField] private Button _claimButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private IAchievementSystem _achievementSystem;
        private ILocalizationSystem _locSystem;
        private string _currentId;

        private void Start()
        {
            _achievementSystem = ServiceLocator.Get<IAchievementSystem>();
            _locSystem = ServiceLocator.Get<ILocalizationSystem>();

            if (_claimButton != null) _claimButton.onClick.AddListener(OnClaim);
            SetVisible(false);
        }

        public void Show(IAchievementInstance achievement)
        {
            _currentId = achievement.AchievementId;

            if (_titleText != null) _titleText.text = _locSystem != null ? _locSystem.GetText(achievement.DisplayName) : achievement.DisplayName.ToString();
            if (_descText != null) _descText.text = _locSystem != null ? _locSystem.GetText(achievement.Description) : achievement.Description.ToString();
            if (_progressText != null) _progressText.text = $"{achievement.CurrentAmount}/{achievement.RequiredAmount}";
            if (_progressFill != null) _progressFill.fillAmount = achievement.Progress;
            if (_tierText != null) _tierText.text = $"Tier {achievement.CurrentTier + 1}/{achievement.MaxTier}";

            UpdateTierList(achievement);
            if (_claimButton != null) _claimButton.gameObject.SetActive(achievement.State == AchievementState.Completed);

            SetVisible(true);
        }

        private void UpdateTierList(IAchievementInstance achievement)
        {
            if (_tierListParent == null || _tierEntryPrefab == null) return;

            for (int i = _tierListParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_tierListParent.GetChild(i).gameObject);
            }

            IReadOnlyList<AchievementTierData> tiers = achievement.GetTiers();
            if (tiers == null) return;

            for (int i = 0; i < tiers.Count; i++)
            {
                GameObject entry = Instantiate(_tierEntryPrefab, _tierListParent);
                Text text = entry.GetComponentInChildren<Text>();
                if (text != null)
                {
                    string status = i < achievement.CurrentTier ? "[Done]" : (i == achievement.CurrentTier ? "[Current]" : "");
                    text.text = $"Tier {i + 1}: {tiers[i].RequiredAmount} {status}";
                }
            }
        }

        private void OnClaim()
        {
            if (string.IsNullOrEmpty(_currentId)) return;
            _achievementSystem.ClaimReward(_currentId);
            SetVisible(false);
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
