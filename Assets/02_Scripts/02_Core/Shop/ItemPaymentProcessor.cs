using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 재화/아이템 차감 결제 처리기 (PaymentType.Item). 교환소·재화 상점용 프레임워크 기본 구현.
    /// product.PaymentId 를 아이템 MID(int)로 파싱해 product.PaymentAmount 만큼 IInventorySystem 에서 차감한다.
    /// 동기 처리(즉시 차감) — 콜백을 그 자리에서 호출한다.
    /// 현금 결제(IAP)·광고(Ad)는 본 처리기 소관이 아니다. IAP 는 IIAPSystem(영수증 검증 seam) 경유로
    /// 별도 처리기(미부팅, 뒤끝 SDK 해소 후 BackndReceiptValidator 연결)로 분리한다 — 의도적 빈칸.
    /// </summary>
    public class ItemPaymentProcessor : IPaymentProcessor
    {
        private readonly IInventorySystem _inventory;

        public PaymentType SupportedType => PaymentType.Item;

        public ItemPaymentProcessor(IInventorySystem inventory)
        {
            _inventory = inventory;
        }

        public void Process(IShopProduct product, Action<PaymentResult> callback)
        {
            if (_inventory == null)
            {
                callback?.Invoke(Fail("inventory_unavailable"));
                return;
            }
            if (product == null)
            {
                callback?.Invoke(Fail("product_null"));
                return;
            }
            if (!int.TryParse(product.PaymentId, out int currencyMid) || currencyMid <= 0)
            {
                callback?.Invoke(Fail("invalid_payment_id"));
                return;
            }

            int price = product.PaymentAmount;
            if (price < 0)
            {
                callback?.Invoke(Fail("invalid_payment_amount"));
                return;
            }

            if (price > 0 && _inventory.GetCount(currencyMid) < price)
            {
                callback?.Invoke(Fail("insufficient_balance"));
                return;
            }

            if (price > 0 && !_inventory.ConsumeByMID(currencyMid, price))
            {
                callback?.Invoke(Fail("consume_failed"));
                return;
            }

            string txId = $"item_{currencyMid}x{price}";
            Debug.Log($"[상점] 재화 차감 완료: {currencyMid} x{price} (거래ID: {txId})");
            callback?.Invoke(new PaymentResult
            {
                Success = true,
                ProviderTransactionId = txId,
                Reason = null
            });
        }

        private static PaymentResult Fail(string reason) => new PaymentResult
        {
            Success = false,
            ProviderTransactionId = null,
            Reason = reason
        };
    }
}
