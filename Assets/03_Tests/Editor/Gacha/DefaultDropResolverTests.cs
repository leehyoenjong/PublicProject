using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Gacha
{
    public class DefaultDropResolverTests
    {
        private DefaultDropResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new DefaultDropResolver();
            Random.InitState(42);
        }

        private static FakeGacha MakeStandardGacha(int pityHard = 0, int pityPickup = 0, bool bonus11th = false, GuaranteedTier guaranteed = GuaranteedTier.None)
        {
            return new FakeGacha
            {
                MID = "gacha_test",
                PityHardCount = pityHard,
                PityPickupCount = pityPickup,
                Bonus11th = bonus11th,
                BonusGuaranteedTier = guaranteed,
                Tiers = new List<GachaTierEntry>
                {
                    TestHelpers.MakeTier(GachaTierRank.SSR, 3),
                    TestHelpers.MakeTier(GachaTierRank.SR, 17),
                    TestHelpers.MakeTier(GachaTierRank.R, 80),
                },
                Drops = new List<GachaDropEntry>
                {
                    TestHelpers.MakeDrop(GachaTierRank.SSR, 10101, 50),
                    TestHelpers.MakeDrop(GachaTierRank.SSR, 10102, 50),
                    TestHelpers.MakeDrop(GachaTierRank.SR, 10201, 100),
                    TestHelpers.MakeDrop(GachaTierRank.R, 10301, 100),
                }
            };
        }

        [Test]
        public void Resolve_SinglePull_ReturnsOneResult()
        {
            var gacha = MakeStandardGacha();
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 1);

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void Resolve_TenPull_NoBonus_ReturnsTenResults()
        {
            var gacha = MakeStandardGacha(bonus11th: false);
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 10);

            Assert.AreEqual(10, results.Count);
        }

        [Test]
        public void Resolve_TenPull_WithBonus11th_ReturnsElevenResults()
        {
            var gacha = MakeStandardGacha(bonus11th: true);
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 10);

            Assert.AreEqual(11, results.Count);
        }

        [Test]
        public void Resolve_HardPityReached_ForcesSSR()
        {
            var gacha = MakeStandardGacha(pityHard: 90);
            var state = new PityCounterState(sinceSSR: 89, sincePickup: 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 1);

            Assert.AreEqual(GachaTierRank.SSR, results[0].Tier);
            Assert.IsTrue(results[0].TriggeredHardPity);
        }

        [Test]
        public void Resolve_PickupPityReached_ForcesPickup()
        {
            var gacha = MakeStandardGacha(pityHard: 90, pityPickup: 180);
            var state = new PityCounterState(sinceSSR: 0, sincePickup: 179);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 1);

            Assert.AreEqual(GachaTierRank.SSR, results[0].Tier);
            Assert.IsTrue(results[0].TriggeredPickupPity);
        }

        [Test]
        public void Resolve_PityDisabledAtZero_NoForce()
        {
            var gacha = MakeStandardGacha(pityHard: 0, pityPickup: 0);
            var state = new PityCounterState(sinceSSR: 999, sincePickup: 999);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 1);

            Assert.IsFalse(results[0].TriggeredHardPity);
            Assert.IsFalse(results[0].TriggeredPickupPity);
        }

        [Test]
        public void Resolve_GuaranteedTierBonus_UpgradesLastSlotIfMissing()
        {
            // R 만 당첨되도록 편향 — weight 분배를 바꿔 SR/SSR weight = 0 으로 강제
            var gacha = new FakeGacha
            {
                MID = "gacha_test",
                BonusGuaranteedTier = GuaranteedTier.SR,
                Tiers = new List<GachaTierEntry>
                {
                    TestHelpers.MakeTier(GachaTierRank.R, 100),
                },
                Drops = new List<GachaDropEntry>
                {
                    TestHelpers.MakeDrop(GachaTierRank.SR, 10201, 100),
                    TestHelpers.MakeDrop(GachaTierRank.R, 10301, 100),
                }
            };
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 10);

            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(GachaTierRank.SR, results[results.Count - 1].Tier, "last slot should be upgraded to SR");
        }

        [Test]
        public void Resolve_GuaranteedTierBonus_NoUpgradeIfAlreadyPresent()
        {
            // SR 만 당첨되도록 편향 — Tier SR weight 만 존재
            var gacha = new FakeGacha
            {
                MID = "gacha_test",
                BonusGuaranteedTier = GuaranteedTier.SR,
                Tiers = new List<GachaTierEntry>
                {
                    TestHelpers.MakeTier(GachaTierRank.SR, 100),
                },
                Drops = new List<GachaDropEntry>
                {
                    TestHelpers.MakeDrop(GachaTierRank.SR, 10201, 100),
                }
            };
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 10);

            foreach (GachaRollResult r in results)
            {
                Assert.AreEqual(GachaTierRank.SR, r.Tier);
            }
        }

        [Test]
        public void Resolve_DropsSelectedOnlyFromMatchingTier()
        {
            var gacha = MakeStandardGacha();
            var state = new PityCounterState(0, 0);

            IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, state, 1000);

            foreach (GachaRollResult r in results)
            {
                switch (r.Tier)
                {
                    case GachaTierRank.SSR:
                        Assert.IsTrue(r.ItemMID == 10101 || r.ItemMID == 10102, $"SSR item MID out of pool: {r.ItemMID}");
                        break;
                    case GachaTierRank.SR:
                        Assert.AreEqual(10201, r.ItemMID);
                        break;
                    case GachaTierRank.R:
                        Assert.AreEqual(10301, r.ItemMID);
                        break;
                }
            }
        }

        [Test]
        public void Resolve_LargeSample_TierDistributionRoughlyMatches()
        {
            var gacha = MakeStandardGacha();
            var state = new PityCounterState(0, 0);
            int ssr = 0, sr = 0, r = 0;

            for (int i = 0; i < 1000; i++)
            {
                var s = new PityCounterState(0, 0);
                IReadOnlyList<GachaRollResult> results = _resolver.Resolve(gacha, s, 1);
                switch (results[0].Tier)
                {
                    case GachaTierRank.SSR: ssr++; break;
                    case GachaTierRank.SR: sr++; break;
                    case GachaTierRank.R: r++; break;
                }
            }

            // weight 3/17/80 → 기대 30/170/800. 통계 허용 오차 ±5%p
            Assert.That(ssr, Is.InRange(0, 100), $"SSR count out of tolerance: {ssr}");
            Assert.That(sr, Is.InRange(100, 240), $"SR count out of tolerance: {sr}");
            Assert.That(r, Is.InRange(740, 880), $"R count out of tolerance: {r}");
            Assert.AreEqual(1000, ssr + sr + r);
        }
    }
}
