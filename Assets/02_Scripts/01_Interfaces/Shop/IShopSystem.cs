using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 상점 시스템 계약. 모든 상점 UI 및 구매 로직의 단일 진입점.
    /// 지불 처리는 IPaymentProcessor 위임, 영속화는 IShopRepository 위임.
    /// </summary>
    public interface IShopSystem : IService
    {
        /// <summary>현재 노출 가능한 상품 목록 (활성 + 조건 만족 + 이벤트 기간 내).</summary>
        IReadOnlyList<IShopProduct> GetVisibleProducts(IShopContext context);

        /// <summary>단건 상품 인스턴스 조회. 없으면 null.</summary>
        IShopProductInstance GetInstance(string productMID);

        /// <summary>구매 가능 여부 + 실패 사유.</summary>
        PurchaseEligibility CanPurchase(string productMID, IShopContext context);

        /// <summary>비동기 구매 요청. 결과는 콜백으로.</summary>
        void Purchase(string productMID, IShopContext context, Action<PurchaseResult> callback);
    }

    /// <summary>조건부 노출 / 구매 판단에 필요한 플레이어 상태 제공.</summary>
    public interface IShopContext
    {
        int PlayerLevel { get; }
        bool IsQuestCleared(int questMID);
    }

    /// <summary>구매 가능 여부 결과.</summary>
    public struct PurchaseEligibility
    {
        public bool CanBuy;
        public string BlockReason;
    }

    /// <summary>구매 완료/실패 결과.</summary>
    public struct PurchaseResult
    {
        public bool Success;
        public string ProductMID;
        public string FailureReason;
        public IReadOnlyList<ShopReward> GrantedRewards;
    }
}
