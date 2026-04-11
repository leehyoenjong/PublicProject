using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 개별 버프 아이콘 슬롯 — 아이콘, 남은시간 게이지, 스택 수 표시
    /// </summary>
    public class BuffIconSlot : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _durationFill;
        [SerializeField] private Text _stackText;
        [SerializeField] private Image _categoryBorder;

        [Header("카테고리별 색상")]
        [SerializeField] private Color _positiveColor = Color.green;
        [SerializeField] private Color _negativeColor = Color.red;
        [SerializeField] private Color _neutralColor = Color.gray;

        private IBuffUIData _data;

        public void SetData(IBuffUIData data)
        {
            _data = data;

            if (_iconImage != null && data.Icon != null)
            {
                _iconImage.sprite = data.Icon;
            }

            UpdateStack(data.StackCount);
            UpdateCategoryBorder(data.Category);
        }

        public void UpdateStack(int stack)
        {
            if (_stackText == null) return;

            if (stack > 1)
            {
                _stackText.gameObject.SetActive(true);
                _stackText.text = stack.ToString();
            }
            else
            {
                _stackText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_data == null) return;
            if (_durationFill == null) return;

            _durationFill.fillAmount = _data.RemainingRatio;
        }

        private void UpdateCategoryBorder(BuffCategory category)
        {
            if (_categoryBorder == null) return;

            _categoryBorder.color = category switch
            {
                BuffCategory.Positive => _positiveColor,
                BuffCategory.Negative => _negativeColor,
                _ => _neutralColor
            };
        }
    }
}
