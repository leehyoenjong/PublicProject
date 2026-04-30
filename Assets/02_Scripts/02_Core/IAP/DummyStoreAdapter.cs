using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 개발용 더미 스토어 어댑터. 즉시 성공 반환.
    /// </summary>
    public class DummyStoreAdapter : IStoreAdapter
    {
        private int _transactionCounter;

        public void Initialize(Action onSuccess, Action<string> onFail)
        {
            Debug.Log("[결제] 더미 스토어 초기화됨.");
            onSuccess?.Invoke();
        }

        public void Purchase(string productId, Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail)
        {
            _transactionCounter++;

            var receipt = new PurchaseReceipt
            {
                ProductId = productId,
                TransactionId = $"dummy_{_transactionCounter}",
                ReceiptData = "dummy_receipt_data",
                Platform = StorePlatform.Dummy,
                PurchaseTime = DateTime.UtcNow.ToString("o")
            };

            Debug.Log($"[결제] 더미 구매 성공: {productId}");
            onSuccess?.Invoke(receipt);
        }

        public void RestorePurchases(Action<List<PurchaseReceipt>> onSuccess, Action<PurchaseFailReason> onFail)
        {
            Debug.Log("[결제] 더미 복원: 빈 목록 반환.");
            onSuccess?.Invoke(new List<PurchaseReceipt>());
        }

        public List<PurchaseReceipt> GetPendingPurchases()
        {
            return new List<PurchaseReceipt>();
        }

        public void ConfirmPurchase(string transactionId)
        {
            Debug.Log($"[결제] 더미 구매 확인됨: {transactionId}");
        }

        public void FetchProducts(List<string> productIds, Action<List<IAPProductData>> onSuccess, Action<string> onFail)
        {
            Debug.Log("[결제] 더미 상품 조회: 빈 목록 반환.");
            onSuccess?.Invoke(new List<IAPProductData>());
        }
    }
}
