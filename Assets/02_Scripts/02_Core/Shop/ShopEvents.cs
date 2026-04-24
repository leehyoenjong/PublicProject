using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>구매 요청 발생 — Purchase() 진입 직후.</summary>
    public struct ShopPurchaseRequestedEvent
    {
        public string ProductMID;
        public PaymentType PaymentType;
    }

    /// <summary>구매 성공 — 지불 + 재고 차감 + 보상 지급 완료 후.</summary>
    public struct ShopPurchaseCompletedEvent
    {
        public string ProductMID;
        public PaymentType PaymentType;
        public string ProviderTransactionId;
        public IReadOnlyList<ShopReward> GrantedRewards;
    }

    /// <summary>구매 실패 — 자격 부족/지불 실패/보상 지급 실패 등.</summary>
    public struct ShopPurchaseFailedEvent
    {
        public string ProductMID;
        public string Reason;
    }

    /// <summary>상품 재고 또는 구매 카운트 변경.</summary>
    public struct ShopStockChangedEvent
    {
        public string ProductMID;
        public int TotalPurchaseCount;
        public int CurrentScopePurchaseCount;
        public bool IsSoldOut;
    }

    /// <summary>상점 갱신(리셋) 발생 — scope 경계 도달.</summary>
    public struct ShopResetEvent
    {
        public LimitScope Scope;
    }
}
