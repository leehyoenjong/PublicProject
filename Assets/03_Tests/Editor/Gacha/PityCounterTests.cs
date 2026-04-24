using NUnit.Framework;

namespace PublicFramework.Tests.Gacha
{
    public class PityCounterTests
    {
        [Test]
        public void NewCounter_HasZeroState()
        {
            var counter = new PityCounter("test");
            Assert.AreEqual("test", counter.GachaMID);
            Assert.AreEqual(0, counter.PullsSinceLastSSR);
            Assert.AreEqual(0, counter.PullsSinceLastPickup);
            Assert.AreEqual(0, counter.TotalPullCount);
            Assert.AreEqual(0L, counter.LastPullAtUtc);
        }

        [Test]
        public void ApplyRoll_SSR_ResetsPullsSinceLastSSR()
        {
            var counter = new PityCounter("test");
            counter.ApplyRoll(GachaTierRank.R, wasPickup: false);
            counter.ApplyRoll(GachaTierRank.R, wasPickup: false);
            counter.ApplyRoll(GachaTierRank.SSR, wasPickup: false);

            Assert.AreEqual(0, counter.PullsSinceLastSSR);
            Assert.AreEqual(3, counter.TotalPullCount);
        }

        [Test]
        public void ApplyRoll_NonSSR_IncrementsPullsSinceLastSSR()
        {
            var counter = new PityCounter("test");
            counter.ApplyRoll(GachaTierRank.R, false);
            counter.ApplyRoll(GachaTierRank.SR, false);

            Assert.AreEqual(2, counter.PullsSinceLastSSR);
        }

        [Test]
        public void ApplyRoll_WithPickup_ResetsPickupCounter()
        {
            var counter = new PityCounter("test");
            counter.ApplyRoll(GachaTierRank.R, false);
            counter.ApplyRoll(GachaTierRank.R, false);
            counter.ApplyRoll(GachaTierRank.SSR, wasPickup: true);

            Assert.AreEqual(0, counter.PullsSinceLastPickup);
            Assert.AreEqual(0, counter.PullsSinceLastSSR);
        }

        [Test]
        public void ApplyRoll_WithoutPickup_IncrementsPickupCounter()
        {
            var counter = new PityCounter("test");
            counter.ApplyRoll(GachaTierRank.SSR, wasPickup: false);

            Assert.AreEqual(1, counter.PullsSinceLastPickup);
            Assert.AreEqual(0, counter.PullsSinceLastSSR);
        }

        [Test]
        public void ToStateFromState_RoundTripPreservesValues()
        {
            var counter = new PityCounter("test");
            counter.ApplyRoll(GachaTierRank.R, false);
            counter.ApplyRoll(GachaTierRank.R, false);

            PityCounterState state = counter.ToState();
            Assert.AreEqual(2, state.PullsSinceLastSSR);
            Assert.AreEqual(2, state.PullsSinceLastPickup);

            state.PullsSinceLastSSR = 50;
            state.PullsSinceLastPickup = 100;
            counter.FromState(state);

            Assert.AreEqual(50, counter.PullsSinceLastSSR);
            Assert.AreEqual(100, counter.PullsSinceLastPickup);
        }

        [Test]
        public void SetLastPullAt_UpdatesTimestamp()
        {
            var counter = new PityCounter("test");
            counter.SetLastPullAt(1700000000L);
            Assert.AreEqual(1700000000L, counter.LastPullAtUtc);
        }

        [Test]
        public void RestoreConstructor_PreservesAllFields()
        {
            var counter = new PityCounter("test", pullsSinceSSR: 5, pullsSincePickup: 10, total: 15, lastPullAtUtc: 9999L);
            Assert.AreEqual("test", counter.GachaMID);
            Assert.AreEqual(5, counter.PullsSinceLastSSR);
            Assert.AreEqual(10, counter.PullsSinceLastPickup);
            Assert.AreEqual(15, counter.TotalPullCount);
            Assert.AreEqual(9999L, counter.LastPullAtUtc);
        }
    }
}
