using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 개발용 더미 영수증 검증. 항상 유효.
    /// </summary>
    public class AlwaysValidReceiptValidator : IReceiptValidator
    {
        public bool SupportsLocalValidation => true;

        public void Validate(PurchaseReceipt receipt, Action<ReceiptValidationResult> callback)
        {
            Debug.Log($"[AlwaysValidValidator] Validated: {receipt.TransactionId}");

            callback?.Invoke(new ReceiptValidationResult
            {
                IsValid = true,
                Error = ReceiptValidationError.None,
                TransactionId = receipt.TransactionId
            });
        }
    }
}
