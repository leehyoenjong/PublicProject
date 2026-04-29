using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PublicFramework.Tests.Shop
{
    public class ShopSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeShopRepository _repo;
        private FakeInventorySystem _inventory;
        private FakeTimeProvider _timeProvider;
        private FakeShopContext _context;
        private FakePaymentProcessor _itemProcessor;
        private ShopSystem _system;

        private ShopData _basic;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _repo = new FakeShopRepository();
            _inventory = new FakeInventorySystem();
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));
            _context = new FakeShopContext { PlayerLevel = 50 };
            _itemProcessor = new FakePaymentProcessor(PaymentType.Item);

            _basic = TestHelpers.MakeShopData(
                "shop_basic",
                PaymentType.Item,
                paymentAmount: 100,
                rewards: new[] { TestHelpers.MakeShopReward(3001, 5) }
            );

            _system = new ShopSystem(_eventBus, _repo, _inventory, _timeProvider);
            _system.Initialize(new List<IShopProduct> { _basic });
            _system.RegisterPaymentProcessor(_itemProcessor);
        }

        // --- 9개 실패 사유 ---

        [Test]
        public void CanPurchase_Unknown_ReturnsProductNotFound()
        {
            PurchaseEligibility e = _system.CanPurchase("nonexistent", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("product_not_found", e.BlockReason);
        }

        [Test]
        public void CanPurchase_Inactive_ReturnsInactive()
        {
            ShopData inactive = TestHelpers.MakeShopData("shop_inactive", isActive: false);
            _system.Initialize(new List<IShopProduct> { inactive });

            PurchaseEligibility e = _system.CanPurchase("shop_inactive", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("inactive", e.BlockReason);
        }

        [Test]
        public void CanPurchase_OutOfEventPeriod_Fails()
        {
            ShopData evt = TestHelpers.MakeShopData(
                "shop_event",
                resetPeriod: ResetPeriod.EventPeriod,
                eventStartUtc: "2027-01-01T00:00:00Z",
                eventEndUtc: "2027-02-01T00:00:00Z"
            );
            _system.Initialize(new List<IShopProduct> { evt });

            PurchaseEligibility e = _system.CanPurchase("shop_event", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("out_of_event_period", e.BlockReason);
        }

        [Test]
        public void CanPurchase_ConditionMinLevelUnmet_Fails()
        {
            ShopData locked = TestHelpers.MakeShopData(
                "shop_locked",
                conditionType: ShopConditionType.MinLevel,
                conditionValue: "100"
            );
            _system.Initialize(new List<IShopProduct> { locked });
            _context.PlayerLevel = 50;

            PurchaseEligibility e = _system.CanPurchase("shop_locked", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("condition_not_met", e.BlockReason);
        }

        [Test]
        public void CanPurchase_ConditionQuestCleared_MetAllowsBuy()
        {
            ShopData q = TestHelpers.MakeShopData(
                "shop_quest",
                conditionType: ShopConditionType.QuestClear,
                conditionValue: "1001"
            );
            _system.Initialize(new List<IShopProduct> { q });
            _system.RegisterPaymentProcessor(_itemProcessor);
            _context.MarkQuestCleared(1001);

            PurchaseEligibility e = _system.CanPurchase("shop_quest", _context);
            Assert.IsTrue(e.CanBuy);
        }

        [Test]
        public void CanPurchase_SoldOut_Fails()
        {
            ShopData limited = TestHelpers.MakeShopData("shop_limited", productLimit: 1);
            _system.Initialize(new List<IShopProduct> { limited });
            _system.RegisterPaymentProcessor(_itemProcessor);

            // 한번 구매로 소진
            _system.Purchase("shop_limited", _context, _ => { });

            PurchaseEligibility e = _system.CanPurchase("shop_limited", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("sold_out", e.BlockReason);
        }

        [Test]
        public void CanPurchase_PlayerLimitReached_Fails()
        {
            ShopData limited = TestHelpers.MakeShopData(
                "shop_daily",
                playerLimit: 1,
                playerLimitScope: LimitScope.Day
            );
            _system.Initialize(new List<IShopProduct> { limited });
            _system.RegisterPaymentProcessor(_itemProcessor);

            _system.Purchase("shop_daily", _context, _ => { });

            PurchaseEligibility e = _system.CanPurchase("shop_daily", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("player_limit_reached", e.BlockReason);
        }

        [Test]
        public void CanPurchase_ProcessorMissing_Fails()
        {
            ShopData iap = TestHelpers.MakeShopData("shop_iap", paymentType: PaymentType.IAP);
            _system.Initialize(new List<IShopProduct> { iap });
            // IAP 프로세서 미등록

            PurchaseEligibility e = _system.CanPurchase("shop_iap", _context);
            Assert.IsFalse(e.CanBuy);
            Assert.AreEqual("payment_processor_missing", e.BlockReason);
        }

        [Test]
        public void Purchase_PaymentFails_PublishesFailedEvent()
        {
            _itemProcessor.NextSuccess = false;
            _itemProcessor.NextFailureReason = "insufficient_funds";

            PurchaseResult captured = default;
            _system.Purchase("shop_basic", _context, r => captured = r);

            Assert.IsFalse(captured.Success);
            Assert.AreEqual("insufficient_funds", captured.FailureReason);
            Assert.AreEqual(1, _eventBus.GetPublished<ShopPurchaseFailedEvent>().Count);
            Assert.AreEqual(0, _eventBus.GetPublished<ShopPurchaseCompletedEvent>().Count);
        }

        [Test]
        public void CanPurchase_NormalState_Succeeds()
        {
            PurchaseEligibility e = _system.CanPurchase("shop_basic", _context);
            Assert.IsTrue(e.CanBuy);
            Assert.IsNull(e.BlockReason);
        }

        // --- 구매 성공 흐름 ---

        [Test]
        public void Purchase_Success_GrantsRewardsAndPublishesEvents()
        {
            PurchaseResult captured = default;
            _system.Purchase("shop_basic", _context, r => captured = r);

            Assert.IsTrue(captured.Success);
            Assert.AreEqual("shop_basic", captured.ProductMID);

            // 보상 지급 (FakeInventorySystem.AddItem 호출)
            Assert.AreEqual(1, _inventory.AddCalls.Count);
            Assert.AreEqual(3001, _inventory.AddCalls[0].mid);
            Assert.AreEqual(5, _inventory.AddCalls[0].count);

            // 이벤트 발행
            Assert.AreEqual(1, _eventBus.GetPublished<ShopPurchaseRequestedEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<ShopPurchaseCompletedEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<ShopStockChangedEvent>().Count);
            Assert.AreEqual(0, _eventBus.GetPublished<ShopPurchaseFailedEvent>().Count);

            Assert.AreEqual(1, _itemProcessor.ProcessCallCount);
        }

        [Test]
        public void Purchase_Success_IncrementsInstanceCountsAndSaves()
        {
            _system.Purchase("shop_basic", _context, _ => { });

            IShopProductInstance inst = _system.GetInstance("shop_basic");
            Assert.AreEqual(1, inst.TotalPurchaseCount);
            Assert.AreEqual(1, inst.CurrentScopePurchaseCount);

            Assert.AreEqual(1, _repo.SaveCallCount);
        }

        [Test]
        public void Purchase_Success_StockChangedEventHasCounts()
        {
            _system.Purchase("shop_basic", _context, _ => { });

            var evt = _eventBus.GetPublished<ShopStockChangedEvent>()[0];
            Assert.AreEqual("shop_basic", evt.ProductMID);
            Assert.AreEqual(1, evt.TotalPurchaseCount);
            Assert.AreEqual(1, evt.CurrentScopePurchaseCount);
            Assert.IsFalse(evt.IsSoldOut);
        }

        // --- GetVisibleProducts ---

        [Test]
        public void GetVisibleProducts_ActiveOnly()
        {
            ShopData active = TestHelpers.MakeShopData("a", isActive: true);
            ShopData inactive = TestHelpers.MakeShopData("b", isActive: false);
            _system.Initialize(new List<IShopProduct> { active, inactive });

            var visible = _system.GetVisibleProducts(_context);

            Assert.AreEqual(1, visible.Count);
            Assert.AreEqual("a", visible[0].MID);
        }

        [Test]
        public void GetVisibleProducts_ConditionFails_Hidden()
        {
            ShopData locked = TestHelpers.MakeShopData(
                "locked",
                conditionType: ShopConditionType.MinLevel,
                conditionValue: "100"
            );
            _system.Initialize(new List<IShopProduct> { locked });
            _context.PlayerLevel = 50;

            var visible = _system.GetVisibleProducts(_context);
            Assert.AreEqual(0, visible.Count);
        }

        // --- Scope reset via time advance ---

        [Test]
        public void ScopeReset_DailyBoundaryCrossed_ResetsScopeCounts()
        {
            ShopData daily = TestHelpers.MakeShopData(
                "daily",
                playerLimit: 1,
                playerLimitScope: LimitScope.Day,
                resetPeriod: ResetPeriod.Daily
            );
            _system.Initialize(new List<IShopProduct> { daily });
            _system.RegisterPaymentProcessor(_itemProcessor);

            _system.Purchase("daily", _context, _ => { });
            Assert.AreEqual(1, _system.GetInstance("daily").CurrentScopePurchaseCount);

            // 다음날 UTC 09:01 로 점프 (리셋 경계 > 09:00)
            _timeProvider.Set(new DateTime(2026, 5, 16, 9, 1, 0, DateTimeKind.Utc));

            // GetVisibleProducts 내부에서 TryAdvanceScopeReset 발화
            _system.GetVisibleProducts(_context);

            IShopProductInstance inst = _system.GetInstance("daily");
            Assert.AreEqual(0, inst.CurrentScopePurchaseCount, "scope count 가 0 으로 리셋");
            Assert.AreEqual(1, inst.TotalPurchaseCount, "총 구매 횟수는 유지");

            var resetEvents = _eventBus.GetPublished<ShopResetEvent>();
            Assert.GreaterOrEqual(resetEvents.Count, 1);
            Assert.AreEqual(LimitScope.Day, resetEvents[0].Scope);
        }

        [Test]
        public void ScopeReset_CallsRepositoryResetScope()
        {
            ShopData daily = TestHelpers.MakeShopData(
                "daily",
                playerLimit: 1,
                playerLimitScope: LimitScope.Day,
                resetPeriod: ResetPeriod.Daily
            );
            _system.Initialize(new List<IShopProduct> { daily });
            _system.RegisterPaymentProcessor(_itemProcessor);

            _system.Purchase("daily", _context, _ => { });

            _timeProvider.Set(new DateTime(2026, 5, 16, 9, 1, 0, DateTimeKind.Utc));
            _system.GetVisibleProducts(_context);

            Assert.GreaterOrEqual(_repo.ResetScopeCallCount, 1);
            Assert.AreEqual(LimitScope.Day, _repo.LastResetScope);
        }

        [Test]
        public void Purchase_BecomesSoldOut_StockChangedEventFlags()
        {
            ShopData one = TestHelpers.MakeShopData("one", productLimit: 1);
            _system.Initialize(new List<IShopProduct> { one });
            _system.RegisterPaymentProcessor(_itemProcessor);

            _system.Purchase("one", _context, _ => { });

            var evt = _eventBus.GetPublished<ShopStockChangedEvent>()[0];
            Assert.IsTrue(evt.IsSoldOut);
        }

        [Test]
        public void RegisterPaymentProcessor_Null_Ignored()
        {
            _system.RegisterPaymentProcessor(null);
            // no exception
        }
    }
}
