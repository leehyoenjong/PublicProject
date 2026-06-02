using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PublicFramework
{
    /// <summary>
    /// 상점 상품 1칸. 상품명(MID)·가격(재화 MID x수량)·보상 목록·구매 버튼을 표시한다.
    /// 아이콘/현지화 표시는 게임마다 다르므로 파생 프로젝트 몫(의도적 빈칸) — 비면 MID/placeholder 색.
    /// 구매 버튼 클릭 시 주입된 콜백으로 상품 MID 를 전달한다.
    /// </summary>
    public class ShopSlotView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Color _emptyIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        private string _productMid;
        private Action<string> _onBuy;

        public void Bind(IShopProduct product, bool canBuy, Action<string> onBuy)
        {
            _productMid = product != null ? product.MID : null;
            _onBuy = onBuy;

            if (_nameText != null)
                _nameText.text = _productMid ?? "(null)";

            if (_costText != null)
                _costText.text = product != null ? $"{product.PaymentId} x{product.PaymentAmount}" : "";

            if (_rewardText != null)
                _rewardText.text = BuildRewardText(product);

            if (_iconImage != null)
            {
                Sprite icon = product != null ? product.Icon : null;
                _iconImage.sprite = icon;
                // 아이콘 미할당(의도적 빈칸: 파생 프로젝트가 채움) → placeholder 색
                _iconImage.color = icon != null ? Color.white : _emptyIconColor;
            }

            if (_buyButton != null)
            {
                _buyButton.interactable = canBuy;
                _buyButton.onClick.RemoveListener(OnBuyClicked);
                _buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        private static string BuildRewardText(IShopProduct product)
        {
            if (product == null || product.Rewards == null || product.Rewards.Count == 0)
                return "";

            var sb = new StringBuilder();
            for (int i = 0; i < product.Rewards.Count; i++)
            {
                ShopReward r = product.Rewards[i];
                if (r == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(r.RewardItemMID).Append(" x").Append(r.RewardAmount);
            }
            return sb.ToString();
        }

        private void OnBuyClicked()
        {
            if (!string.IsNullOrEmpty(_productMid)) _onBuy?.Invoke(_productMid);
        }

        private void OnDestroy()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }
}
