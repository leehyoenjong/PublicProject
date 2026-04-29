using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Gacha
{
    public class GachaSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeInventorySystem _inventory;
        private FakeGachaRepository _repository;
        private FakeTimeProvider _timeProvider;
        private FakeGachaContext _context;
        private GachaSystem _system;

        private FakeBanner _banner;
        private FakeGacha _gacha;

        [SetUp]
        public void SetUp()
        {
            Random.InitState(42);

            _eventBus = new FakeEventBus();
            _inventory = new FakeInventorySystem();
            _repository = new FakeGachaRepository();
            _timeProvider = new FakeTimeProvider(new System.DateTime(2026, 5, 15, 12, 0, 0, System.DateTimeKind.Utc));
            _context = new FakeGachaContext { PlayerLevel = 50 };

            _gacha = new FakeGacha
            {
                MID = "gacha_test",
                Cost1Item = 9001,
                Cost1Amount = 100,
                Cost10Item = 9001,
                Cost10Amount = 900,
                Tiers = new List<GachaTierEntry>
                {
                    TestHelpers.MakeTier(GachaTierRank.SSR, 3),
                    TestHelpers.MakeTier(GachaTierRank.SR, 17),
                    TestHelpers.MakeTier(GachaTierRank.R, 80),
                },
                Drops = new List<GachaDropEntry>
                {
                    TestHelpers.MakeDrop(GachaTierRank.SSR, 10101, 100),
                    TestHelpers.MakeDrop(GachaTierRank.SR, 10201, 100),
                    TestHelpers.MakeDrop(GachaTierRank.R, 10301, 100),
                }
            };

            _banner = new FakeBanner
            {
                MID = "banner_test",
                IsActive = true,
                Gachas = new List<BannerGachaEntry> { TestHelpers.MakeBannerGacha("gacha_test") }
            };

            _system = new GachaSystem(_eventBus, _inventory, _repository, _timeProvider);
            _system.Initialize(new List<IBanner> { _banner }, new List<IGacha> { _gacha });

            _inventory.SetBalance(9001, 10000);
        }

        [Test]
        public void CanPull_UnknownGacha_ReturnsGachaNotFound()
        {
            PullEligibility eligibility = _system.CanPull("nonexistent", 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("gacha_not_found", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_InvalidCount_Fails()
        {
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 3, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("invalid_pull_count", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_InactiveGacha_Fails()
        {
            _gacha.IsActive = false;
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("inactive", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_InactiveBanner_Fails()
        {
            _banner.IsActive = false;
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("banner_inactive", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_BannerLockedByMinLevel_Fails()
        {
            _banner.UnlockType = BannerUnlockType.MinLevel;
            _banner.UnlockValue = "100";
            _context.PlayerLevel = 50;
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("banner_locked", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_InsufficientCurrency_Fails()
        {
            _inventory.SetBalance(9001, 50);
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("insufficient_currency", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_DailyLimitExceeded_Fails()
        {
            _gacha.DailyLimit = 5;
            _repository.SetPurchaseCount(_gacha.MID, PurchaseScope.Daily, 5);
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("daily_limit_exceeded", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_LifetimeLimitExceeded_Fails()
        {
            _gacha.LifetimeLimit = 100;
            _repository.SetPurchaseCount(_gacha.MID, PurchaseScope.Lifetime, 100);
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsFalse(eligibility.CanPull);
            Assert.AreEqual("lifetime_limit_exceeded", eligibility.BlockReason);
        }

        [Test]
        public void CanPull_NormalState_Succeeds()
        {
            PullEligibility eligibility = _system.CanPull(_gacha.MID, 1, _context);
            Assert.IsTrue(eligibility.CanPull);
            Assert.IsNull(eligibility.BlockReason);
        }

        [Test]
        public void Pull_Success_ConsumesCurrencyAndPublishesEvents()
        {
            PullResult capturedResult = default;
            _system.Pull(_gacha.MID, 1, _context, r => capturedResult = r);

            Assert.IsTrue(capturedResult.Success);
            Assert.AreEqual(_gacha.MID, capturedResult.GachaMID);
            Assert.AreEqual(1, capturedResult.Rewards.Count);

            // Currency consumed
            Assert.AreEqual(9900, _inventory.GetCount(9001));

            // Events: Requested + Completed
            Assert.AreEqual(1, _eventBus.GetPublished<GachaPullRequestedEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<GachaPullCompletedEvent>().Count);
            Assert.AreEqual(0, _eventBus.GetPublished<GachaPullFailedEvent>().Count);
        }

        [Test]
        public void Pull_Success_IncrementsPurchaseCounts()
        {
            _system.Pull(_gacha.MID, 1, _context, _ => { });

            Assert.AreEqual(1, _repository.GetPurchaseCount(_gacha.MID, PurchaseScope.Daily));
            Assert.AreEqual(1, _repository.GetPurchaseCount(_gacha.MID, PurchaseScope.Period));
            Assert.AreEqual(1, _repository.GetPurchaseCount(_gacha.MID, PurchaseScope.Lifetime));
        }

        [Test]
        public void Pull_Success_SavesPityCounter()
        {
            _system.Pull(_gacha.MID, 1, _context, _ => { });

            IReadOnlyList<IPityCounter> saved = _repository.LoadAll();
            Assert.AreEqual(1, saved.Count);
            Assert.AreEqual(_gacha.MID, saved[0].GachaMID);
            Assert.AreEqual(1, saved[0].TotalPullCount);
        }

        [Test]
        public void Pull_TenPull_ConsumesMultiCost()
        {
            _system.Pull(_gacha.MID, 10, _context, _ => { });

            // 10연 재화 900 차감
            Assert.AreEqual(9100, _inventory.GetCount(9001));
        }

        [Test]
        public void Pull_Success_CompletedEventHasBannerMID()
        {
            _system.Pull(_gacha.MID, 1, _context, _ => { });

            var events = _eventBus.GetPublished<GachaPullCompletedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(_banner.MID, events[0].BannerMID);
        }

        [Test]
        public void Pull_Fail_InsufficientCurrency_PublishesFailedEvent()
        {
            _inventory.SetBalance(9001, 0);
            PullResult captured = default;
            _system.Pull(_gacha.MID, 1, _context, r => captured = r);

            Assert.IsFalse(captured.Success);
            Assert.AreEqual("insufficient_currency", captured.FailureReason);
            Assert.AreEqual(1, _eventBus.GetPublished<GachaPullFailedEvent>().Count);
            Assert.AreEqual(0, _eventBus.GetPublished<GachaPullCompletedEvent>().Count);
        }

        [Test]
        public void Pull_HardPityBoundary_PublishesPityTriggeredEvent()
        {
            _gacha.PityHardCount = 2;
            _repository.Save(new PityCounter(_gacha.MID, pullsSinceSSR: 1, pullsSincePickup: 0, total: 1, lastPullAtUtc: 0L));
            _system.Initialize(new List<IBanner> { _banner }, new List<IGacha> { _gacha }); // reload counters

            _system.Pull(_gacha.MID, 1, _context, _ => { });

            var pityEvents = _eventBus.GetPublished<GachaPityTriggeredEvent>();
            Assert.AreEqual(1, pityEvents.Count);
            Assert.IsTrue(pityEvents[0].HardPity);
        }

        [Test]
        public void GetBanner_ExistingMID_ReturnsBanner()
        {
            IBanner b = _system.GetBanner(_banner.MID);
            Assert.AreSame(_banner, b);
        }

        [Test]
        public void GetBanner_UnknownMID_ReturnsNull()
        {
            IBanner b = _system.GetBanner("nonexistent");
            Assert.IsNull(b);
        }

        [Test]
        public void GetGacha_ExistingMID_ReturnsGacha()
        {
            IGacha g = _system.GetGacha(_gacha.MID);
            Assert.AreSame(_gacha, g);
        }

        [Test]
        public void GetVisibleBanners_ActiveBanner_Returned()
        {
            IReadOnlyList<IBanner> visible = _system.GetVisibleBanners(_context);
            Assert.AreEqual(1, visible.Count);
            Assert.AreSame(_banner, visible[0]);
        }

        [Test]
        public void GetVisibleBanners_InactiveBanner_Hidden()
        {
            _banner.IsActive = false;
            IReadOnlyList<IBanner> visible = _system.GetVisibleBanners(_context);
            Assert.AreEqual(0, visible.Count);
        }

        [Test]
        public void GetVisibleBanners_LockedByLevel_Hidden()
        {
            _banner.UnlockType = BannerUnlockType.MinLevel;
            _banner.UnlockValue = "100";
            _context.PlayerLevel = 50;

            IReadOnlyList<IBanner> visible = _system.GetVisibleBanners(_context);
            Assert.AreEqual(0, visible.Count);
        }
    }
}
