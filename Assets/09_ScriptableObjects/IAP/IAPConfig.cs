using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 IAP/광고 설정.
    /// 상품 카탈로그 및 광고 슬롯 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "IAPConfig", menuName = "PublicFramework/IAP/IAPConfig")]
    public class IAPConfig : ScriptableObject
    {
        [Header("IAP 상품")]
        [SerializeField] private IAPProductData[] _products;

        [Header("광고 슬롯")]
        [SerializeField] private AdSlotData[] _adSlots;

        public IAPProductData[] GetProducts()
        {
            if (_products == null) return Array.Empty<IAPProductData>();

            var copy = new IAPProductData[_products.Length];
            Array.Copy(_products, copy, _products.Length);
            return copy;
        }

        public IAPProductData GetProduct(string productId)
        {
            if (_products == null) return null;

            foreach (IAPProductData product in _products)
            {
                if (product.ProductId == productId) return product;
            }

            return null;
        }

        public AdSlotData GetAdSlot(string slotId)
        {
            if (_adSlots == null) return null;

            foreach (AdSlotData slot in _adSlots)
            {
                if (slot.SlotId == slotId) return slot;
            }

            return null;
        }

        public AdSlotData[] GetAdSlots()
        {
            if (_adSlots == null) return Array.Empty<AdSlotData>();

            var copy = new AdSlotData[_adSlots.Length];
            Array.Copy(_adSlots, copy, _adSlots.Length);
            return copy;
        }
    }
}
