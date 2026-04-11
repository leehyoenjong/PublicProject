using System;

namespace PublicFramework
{
    /// <summary>
    /// 영수증 검증 인터페이스.
    /// 서버 또는 로컬 검증을 추상화한다.
    /// </summary>
    public interface IReceiptValidator
    {
        void Validate(PurchaseReceipt receipt, Action<ReceiptValidationResult> callback);
        bool SupportsLocalValidation { get; }
    }
}
