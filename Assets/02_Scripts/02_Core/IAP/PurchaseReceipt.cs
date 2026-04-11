using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 구매 영수증. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class PurchaseReceipt
    {
        [SerializeField] private string _productId;
        [SerializeField] private string _transactionId;
        [SerializeField] private string _receiptData;
        [SerializeField] private StorePlatform _platform;
        [SerializeField] private string _purchaseTime;

        public string ProductId { get => _productId; set => _productId = value; }
        public string TransactionId { get => _transactionId; set => _transactionId = value; }
        public string ReceiptData { get => _receiptData; set => _receiptData = value; }
        public StorePlatform Platform { get => _platform; set => _platform = value; }
        public string PurchaseTime { get => _purchaseTime; set => _purchaseTime = value; }
    }
}
