using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 슬롯 UI
    /// </summary>
    public class QuestSlotUI : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private GameObject _completedIcon;
        [SerializeField] private Button _button;

        private ILocalizationSystem _locSystem;

        public void SetData(IQuestInstance quest, Action onClick)
        {
            if (_locSystem == null) _locSystem = ServiceLocator.Get<ILocalizationSystem>();

            if (_nameText != null)
            {
                _nameText.text = _locSystem != null ? _locSystem.GetText(quest.DisplayName) : quest.DisplayName.ToString();
            }

            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(quest.Progress * 100f)}%";
            }

            if (_progressFill != null) _progressFill.fillAmount = quest.Progress;
            if (_completedIcon != null) _completedIcon.SetActive(quest.State == QuestState.Completed);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}
