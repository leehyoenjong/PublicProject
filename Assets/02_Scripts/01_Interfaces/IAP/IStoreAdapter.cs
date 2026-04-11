using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 플랫폼 스토어 추상화 인터페이스.
    /// Google Play, App Store 등 실제 스토어 연동을 추상화한다.
    /// </summary>
    public interface IStoreAdapter
    {
        void Initialize(Action onSuccess, Action<string> onFail);
        void Purchase(string productId, Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail);
        void RestorePurchases(Action<List<PurchaseReceipt>> onSuccess, Action<PurchaseFailReason> onFail);
        List<PurchaseReceipt> GetPendingPurchases();
        void ConfirmPurchase(string transactionId);
        void FetchProducts(List<string> productIds, Action<List<IAPProductData>> onSuccess, Action<string> onFail);
    }
}
