using NUnit.Framework;

namespace PublicFramework.Tests.Achievement
{
    public class AchievementInstanceTests
    {
        private AchievementData MakeMultiTier()
        {
            var t1 = TestHelpers.MakeAchievementTier(5);
            var t2 = TestHelpers.MakeAchievementTier(20);
            var t3 = TestHelpers.MakeAchievementTier(100);
            return TestHelpers.MakeAchievementData("a1", ConditionType.Kill, "slime", new[] { t1, t2, t3 });
        }

        [Test]
        public void NewInstance_StartsInProgressAtTierZero()
        {
            var instance = new AchievementInstance(MakeMultiTier());

            Assert.AreEqual(AchievementState.InProgress, instance.State);
            Assert.AreEqual(0, instance.CurrentTier);
            Assert.AreEqual(3, instance.MaxTier);
            Assert.AreEqual(5, instance.RequiredAmount);
            Assert.AreEqual(0, instance.CurrentAmount);
        }

        [Test]
        public void AddProgress_BelowThreshold_StaysInProgress()
        {
            var instance = new AchievementInstance(MakeMultiTier());
            instance.AddProgress(3);

            Assert.AreEqual(AchievementState.InProgress, instance.State);
            Assert.AreEqual(3, instance.CurrentAmount);
            Assert.AreEqual(0.6f, instance.Progress, 0.0001f);
        }

        [Test]
        public void AddProgress_CrossingThreshold_TransitionsToCompleted()
        {
            var instance = new AchievementInstance(MakeMultiTier());
            instance.AddProgress(5);

            Assert.AreEqual(AchievementState.Completed, instance.State);
        }

        [Test]
        public void ClaimCurrentTier_NotCompleted_ReturnsFalse()
        {
            var instance = new AchievementInstance(MakeMultiTier());
            Assert.IsFalse(instance.ClaimCurrentTier());
        }

        [Test]
        public void ClaimCurrentTier_Completed_AdvancesToNextTier()
        {
            var instance = new AchievementInstance(MakeMultiTier());
            instance.AddProgress(5);

            bool result = instance.ClaimCurrentTier();

            Assert.IsTrue(result);
            Assert.AreEqual(1, instance.CurrentTier);
            Assert.AreEqual(AchievementState.InProgress, instance.State);
            Assert.AreEqual(20, instance.RequiredAmount);
        }

        [Test]
        public void ClaimCurrentTier_FinalTier_StateStaysRewarded()
        {
            var instance = new AchievementInstance(MakeMultiTier());
            instance.AddProgress(200); // 전 티어 임계치 넘김, tier 0 Completed
            instance.ClaimCurrentTier(); // tier 1 로 이동, state InProgress (currentAmount 유지)
            instance.AddProgress(0); // tier 1 임계치 재평가 → Completed
            instance.ClaimCurrentTier(); // tier 2 로 이동
            instance.AddProgress(0); // tier 2 임계치 재평가 → Completed
            bool claimed = instance.ClaimCurrentTier();

            Assert.IsTrue(claimed);
            Assert.AreEqual(3, instance.CurrentTier);
            Assert.AreEqual(AchievementState.Rewarded, instance.State);
        }

        [Test]
        public void Progress_NoRequired_Returns100Percent()
        {
            var data = TestHelpers.MakeAchievementData("a1", ConditionType.Kill, "", System.Array.Empty<AchievementTierData>());
            var instance = new AchievementInstance(data);

            Assert.AreEqual(0, instance.MaxTier);
            Assert.AreEqual(0, instance.RequiredAmount);
            Assert.AreEqual(1f, instance.Progress);
        }
    }
}
