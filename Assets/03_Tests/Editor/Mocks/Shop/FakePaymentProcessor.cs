using System;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IPaymentProcessor. 성공/실패를 사전에 지정.</summary>
    public class FakePaymentProcessor : IPaymentProcessor
    {
        public PaymentType SupportedType { get; }
        public bool NextSuccess { get; set; } = true;
        public string NextFailureReason { get; set; }
        public string TransactionId { get; set; } = "tx_fake";
        public int ProcessCallCount { get; private set; }
        public IShopProduct LastProduct { get; private set; }

        public FakePaymentProcessor(PaymentType type)
        {
            SupportedType = type;
        }

        public void Process(IShopProduct product, Action<PaymentResult> callback)
        {
            ProcessCallCount++;
            LastProduct = product;

            callback?.Invoke(new PaymentResult
            {
                Success = NextSuccess,
                ProviderTransactionId = NextSuccess ? TransactionId : null,
                Reason = NextSuccess ? null : NextFailureReason
            });
        }
    }
}
