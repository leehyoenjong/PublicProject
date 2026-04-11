using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// IAP 서비스 인터페이스.
    /// 구매 흐름, 상품 조회, 영수증 복원을 관리한다.
    /// </summary>
    public interface IIAPSystem : IService
    {
        void Purchase(string productId, Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail);
        IReadOnlyList<IAPProductData> GetProducts();
        IAPProductData GetProduct(string productId);
        IReadOnlyList<PurchaseReceipt> GetPurchaseHistory(string productId = null);
        void RestorePurchases(Action<int> onComplete, Action<PurchaseFailReason> onFail);
        SubscriptionState GetSubscriptionState(string productId);
        void ProcessPendingPurchases();
    }
}
