using System;

namespace PublicFramework
{
    /// <summary>
    /// 상품 지불 처리 전략. Ad / IAP / Item 각각 구현체를 프로젝트가 주입.
    /// Item 은 프레임워크 기본 구현(IInventorySystem 연계) 제공 가능,
    /// Ad / IAP 는 외부 SDK 의존성이 있어 프로젝트별로 구현.
    /// </summary>
    public interface IPaymentProcessor
    {
        PaymentType SupportedType { get; }

        /// <summary>
        /// 지불 처리. 결과는 비동기 콜백으로 반환.
        /// 성공 시 PaymentResult.Success=true, ProviderTransactionId 설정.
        /// 실패 시 Reason 에 사유 기입.
        /// </summary>
        void Process(IShopProduct product, Action<PaymentResult> callback);
    }

    /// <summary>지불 처리 결과 DTO.</summary>
    public struct PaymentResult
    {
        public bool Success;
        public string ProviderTransactionId;
        public string Reason;
    }
}
