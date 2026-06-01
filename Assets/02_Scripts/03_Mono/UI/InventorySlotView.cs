using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PublicFramework
{
    /// <summary>
    /// 인벤토리 슬롯 1칸. 아이콘 / 개수 / 라벨(MID·카테고리)을 표시한다.
    /// 아이콘 스프라이트는 게임마다 다르므로(파생 프로젝트 구현 — 의도적 빈칸),
    /// 비어있으면 placeholder 색만 표시한다.
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private Color _emptyIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        public void Bind(int mid, int count, IItem item)
        {
            if (_labelText != null)
                _labelText.text = item != null ? $"{mid}\n{item.Category}" : mid.ToString();

            if (_countText != null)
                _countText.text = count > 1 ? $"x{count}" : (count == 1 ? "" : "x0");

            if (_iconImage != null)
            {
                Sprite icon = item != null ? item.Icon : null;
                _iconImage.sprite = icon;
                // 아이콘 미할당(의도적 빈칸: 파생 프로젝트가 채움) → placeholder 색
                _iconImage.color = icon != null ? Color.white : _emptyIconColor;
            }
        }
    }
}
