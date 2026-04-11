using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IAP 상품 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class IAPProductData
    {
        [SerializeField] private string _productId;
        [SerializeField] private IAPProductType _productType;
        [SerializeField] private string _displayName;
        [SerializeField] private string _priceString;
        [SerializeField] private float _priceAmount;
        [SerializeField] private IAPRewardEntry[] _rewards;
        [SerializeField] private int _purchaseLimit;
        [SerializeField] private SubscriptionData _subscriptionData;

        public string ProductId { get => _productId; set => _productId = value; }
        public IAPProductType ProductType { get => _productType; set => _productType = value; }
        public string DisplayName { get => _displayName; set => _displayName = value; }
        public string PriceString { get => _priceString; set => _priceString = value; }
        public float PriceAmount { get => _priceAmount; set => _priceAmount = value; }
        public IReadOnlyList<IAPRewardEntry> Rewards => _rewards;
        public void SetRewards(IAPRewardEntry[] rewards) { _rewards = rewards; }
        public int PurchaseLimit { get => _purchaseLimit; set => _purchaseLimit = value; }
        public SubscriptionData SubscriptionData { get => _subscriptionData; set => _subscriptionData = value; }
    }
}
