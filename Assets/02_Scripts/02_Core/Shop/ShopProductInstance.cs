namespace PublicFramework
{
    /// <summary>
    /// IShopProductInstance 구현 — 상점 상품의 런타임 상태 DTO.
    /// ShopRepository 가 직렬화하여 영속화한다.
    /// </summary>
    public class ShopProductInstance : IShopProductInstance
    {
        private readonly string _productMID;
        private int _totalPurchaseCount;
        private int _currentScopePurchaseCount;
        private long _lastPurchaseAtUtc;

        public string ProductMID => _productMID;
        public int TotalPurchaseCount => _totalPurchaseCount;
        public int CurrentScopePurchaseCount => _currentScopePurchaseCount;
        public long LastPurchaseAtUtc => _lastPurchaseAtUtc;

        public bool IsSoldOut { get; private set; }

        public ShopProductInstance(string productMID)
        {
            _productMID = productMID;
        }

        public ShopProductInstance(string productMID, int totalPurchaseCount, int currentScopePurchaseCount, long lastPurchaseAtUtc, int productLimit)
        {
            _productMID = productMID;
            _totalPurchaseCount = totalPurchaseCount;
            _currentScopePurchaseCount = currentScopePurchaseCount;
            _lastPurchaseAtUtc = lastPurchaseAtUtc;
            RecalculateSoldOut(productLimit);
        }

        /// <summary>구매 완료 시 카운트 증가 + 타임스탬프 기록.</summary>
        public void RegisterPurchase(long nowUnixSeconds, int productLimit)
        {
            _totalPurchaseCount++;
            _currentScopePurchaseCount++;
            _lastPurchaseAtUtc = nowUnixSeconds;
            RecalculateSoldOut(productLimit);
        }

        /// <summary>scope 경계 도달 시 현재 scope 카운트 리셋.</summary>
        public void ResetScopeCount()
        {
            _currentScopePurchaseCount = 0;
        }

        /// <summary>productLimit 갱신 / 복원 시 품절 상태 재계산.</summary>
        public void RecalculateSoldOut(int productLimit)
        {
            IsSoldOut = productLimit > 0 && _totalPurchaseCount >= productLimit;
        }
    }
}
