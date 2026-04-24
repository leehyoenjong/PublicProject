namespace PublicFramework
{
    /// <summary>
    /// 상점 상품의 런타임 상태. 구매 횟수·마지막 구매 시각 등 시간에 따라 변하는 데이터.
    /// IShopRepository 로 영속화된다.
    /// </summary>
    public interface IShopProductInstance
    {
        string ProductMID { get; }

        /// <summary>평생 구매 횟수 (Lifetime scope 판단용).</summary>
        int TotalPurchaseCount { get; }

        /// <summary>현재 Day/Week scope 내 구매 횟수. scope 리셋 시 0 으로 초기화.</summary>
        int CurrentScopePurchaseCount { get; }

        /// <summary>마지막 구매 시각 (UnixSeconds UTC). 0 = 구매 이력 없음.</summary>
        long LastPurchaseAtUtc { get; }

        /// <summary>productLimit 도달 — 전체 수량 제한.</summary>
        bool IsSoldOut { get; }
    }
}
