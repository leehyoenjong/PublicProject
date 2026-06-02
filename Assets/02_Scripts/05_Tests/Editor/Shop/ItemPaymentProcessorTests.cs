using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Shop
{
    /// <summary>
    /// ItemPaymentProcessor(재화 차감 결제) 단위 검증 —
    /// 잔액 충분/부족/0원/음수가격/잘못된 통화ID/널 인벤토리/널 상품.
    /// IShopProduct 는 인라인 Fake 로 PaymentId/PaymentAmount 만 제어한다.
    /// </summary>
    public class ItemPaymentProcessorTests
    {
        private const int GOLD = 40001;

        private class FakeShopProduct : IShopProduct
        {
            public string MID => "test_product";
            public int DisplayNameKey => 0;
            public int DescriptionKey => 0;
            public Sprite Icon => null;
            public PaymentType PaymentType => PaymentType.Item;
            public string PaymentId { get; set; }
            public int PaymentAmount { get; set; }
            public ResetPeriod ResetPeriod => ResetPeriod.None;
            public DayOfWeekMask WeeklyMask => DayOfWeekMask.None;
            public string EventStartUtc => null;
            public string EventEndUtc => null;
            public int ProductLimit => 0;
            public int PlayerLimit => 0;
            public LimitScope PlayerLimitScope => LimitScope.Day;
            public int DiscountPercent => 0;
            public int FirstPurchaseBonusPercent => 0;
            public ShopConditionType ConditionType => ShopConditionType.None;
            public string ConditionValue => null;
            public int SlotIndex => 0;
            public bool IsFeatured => false;
            public bool IsActive => true;
            public IReadOnlyList<ShopReward> Rewards => null;
        }

        private static FakeShopProduct Product(string paymentId, int amount) =>
            new FakeShopProduct { PaymentId = paymentId, PaymentAmount = amount };

        [Test]
        public void Process_SufficientBalance_SucceedsAndDeducts()
        {
            var inv = new FakeInventorySystem();
            inv.SetBalance(GOLD, 100);
            var sut = new ItemPaymentProcessor(inv);

            PaymentResult result = default;
            sut.Process(Product("40001", 10), r => result = r);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(90, inv.GetCount(GOLD));
            Assert.IsNotNull(result.ProviderTransactionId);
        }

        [Test]
        public void Process_InsufficientBalance_FailsAndKeepsBalance()
        {
            var inv = new FakeInventorySystem();
            inv.SetBalance(GOLD, 5);
            var sut = new ItemPaymentProcessor(inv);

            PaymentResult result = default;
            sut.Process(Product("40001", 10), r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_balance", result.Reason);
            Assert.AreEqual(5, inv.GetCount(GOLD));
        }

        [Test]
        public void Process_InvalidPaymentId_Fails()
        {
            var inv = new FakeInventorySystem();
            var sut = new ItemPaymentProcessor(inv);

            PaymentResult result = default;
            sut.Process(Product("currency_gem", 10), r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("invalid_payment_id", result.Reason);
        }

        [Test]
        public void Process_NullInventory_Fails()
        {
            var sut = new ItemPaymentProcessor(null);

            PaymentResult result = default;
            sut.Process(Product("40001", 10), r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("inventory_unavailable", result.Reason);
        }

        [Test]
        public void Process_ZeroPrice_SucceedsWithoutDeduct()
        {
            var inv = new FakeInventorySystem();
            inv.SetBalance(GOLD, 3);
            var sut = new ItemPaymentProcessor(inv);

            PaymentResult result = default;
            sut.Process(Product("40001", 0), r => result = r);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, inv.GetCount(GOLD));
        }

        [Test]
        public void Process_NegativePrice_FailsAndKeepsBalance()
        {
            var inv = new FakeInventorySystem();
            inv.SetBalance(GOLD, 50);
            var sut = new ItemPaymentProcessor(inv);

            PaymentResult result = new PaymentResult();
            sut.Process(Product("40001", -1), r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("invalid_payment_amount", result.Reason);
            Assert.AreEqual(50, inv.GetCount(GOLD));
        }

        [Test]
        public void Process_NullProduct_Fails()
        {
            var sut = new ItemPaymentProcessor(new FakeInventorySystem());

            PaymentResult result = new PaymentResult();
            sut.Process(null, r => result = r);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("product_null", result.Reason);
        }

        [Test]
        public void SupportedType_IsItem()
        {
            Assert.AreEqual(PaymentType.Item, new ItemPaymentProcessor(null).SupportedType);
        }
    }
}
