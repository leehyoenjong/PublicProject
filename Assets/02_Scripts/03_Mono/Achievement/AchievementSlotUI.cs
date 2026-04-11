using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 업적 슬롯 UI
    /// </summary>
    public class AchievementSlotUI : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Text _tierText;
        [SerializeField] private GameObject _completedIcon;
        [SerializeField] private Button _button;

        public void SetData(IAchievementInstance achievement, Action onClick)
        {
            if (_nameText != null)
            {
                _nameText.text = achievement.IsHidden && achievement.State == AchievementState.InProgress
                    ? "???"
                    : achievement.DisplayName;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{achievement.CurrentAmount}/{achievement.RequiredAmount}";
            }

            if (_progressFill != null) _progressFill.fillAmount = achievement.Progress;

            if (_tierText != null && achievement.MaxTier > 1)
            {
                _tierText.text = $"{achievement.CurrentTier + 1}/{achievement.MaxTier}";
            }

            if (_completedIcon != null) _completedIcon.SetActive(achievement.State == AchievementState.Completed);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}
