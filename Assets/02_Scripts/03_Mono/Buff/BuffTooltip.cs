using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 버프 툴팁 팝업
    /// </summary>
    public class BuffTooltip : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _remainingText;
        [SerializeField] private Text _stackText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private IBuffUIData _currentData;

        public void Show(IBuffUIData data)
        {
            _currentData = data;

            if (_titleText != null) _titleText.text = data.TooltipTitle;
            if (_descText != null) _descText.text = data.TooltipDesc;
            if (_iconImage != null && data.Icon != null) _iconImage.sprite = data.Icon;

            UpdateDynamic();
            SetVisible(true);

            Debug.Log($"[BuffTooltip] Show: {data.BuffId}");
        }

        public void Hide()
        {
            _currentData = null;
            SetVisible(false);
        }

        private void Update()
        {
            if (_currentData == null) return;
            UpdateDynamic();
        }

        private void UpdateDynamic()
        {
            if (_currentData == null) return;

            if (_remainingText != null) _remainingText.text = _currentData.RemainingText;

            if (_stackText != null)
            {
                if (_currentData.StackCount > 1)
                {
                    _stackText.gameObject.SetActive(true);
                    _stackText.text = $"x{_currentData.StackCount}";
                }
                else
                {
                    _stackText.gameObject.SetActive(false);
                }
            }
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
