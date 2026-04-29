using NUnit.Framework;

namespace PublicFramework.Tests.Achievement
{
    public class AchievementSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeSaveSystem _saveSystem;
        private FakeRewardHandler _rewardHandler;
        private AchievementSystem _system;

        private AchievementData MakeAch(string id, ConditionType type = ConditionType.Kill, string target = "slime")
        {
            var t1 = TestHelpers.MakeAchievementTier(5, new[] { new QuestReward(9001, 100) });
            var t2 = TestHelpers.MakeAchievementTier(20, new[] { new QuestReward(9001, 300) });
            var t3 = TestHelpers.MakeAchievementTier(100, new[] { new QuestReward(9001, 1000) });
            return TestHelpers.MakeAchievementData(id, type, target, new[] { t1, t2, t3 });
        }

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _saveSystem = new FakeSaveSystem();
            _rewardHandler = new FakeRewardHandler();
            _system = new AchievementSystem(_eventBus, _saveSystem);
            _system.SetRewardHandler(_rewardHandler);
        }

        [Test]
        public void RegisterAchievement_AddsInInProgressState()
        {
            _system.RegisterAchievement(MakeAch("a1"));

            Assert.AreEqual(AchievementState.InProgress, _system.GetState("a1"));
        }

        [Test]
        public void RegisterAchievement_Null_Ignored()
        {
            _system.RegisterAchievement(null);
            Assert.AreEqual(AchievementState.Locked, _system.GetState("a1"));
        }

        [Test]
        public void RegisterAchievement_Duplicate_Ignored()
        {
            AchievementData data = MakeAch("a1");
            _system.RegisterAchievement(data);
            _system.RegisterAchievement(data);

            Assert.AreEqual(1, _system.GetAchievements().Count);
        }

        [Test]
        public void NotifyProgress_MatchingType_UpdatesProgressEvent()
        {
            _system.RegisterAchievement(MakeAch("a1"));

            _system.NotifyProgress(ConditionType.Kill, "slime", 2);

            var progressEvents = _eventBus.GetPublished<AchievementProgressEvent>();
            Assert.AreEqual(1, progressEvents.Count);
            Assert.AreEqual(2, progressEvents[0].CurrentAmount);
            Assert.AreEqual(5, progressEvents[0].RequiredAmount);
        }

        [Test]
        public void NotifyProgress_DifferentType_Ignored()
        {
            _system.RegisterAchievement(MakeAch("a1", ConditionType.Kill, "slime"));

            _system.NotifyProgress(ConditionType.Collect, "slime", 5);

            Assert.AreEqual(0, _eventBus.GetPublished<AchievementProgressEvent>().Count);
        }

        [Test]
        public void NotifyProgress_DifferentTarget_Ignored()
        {
            _system.RegisterAchievement(MakeAch("a1", ConditionType.Kill, "slime"));

            _system.NotifyProgress(ConditionType.Kill, "dragon", 5);

            Assert.AreEqual(0, _eventBus.GetPublished<AchievementProgressEvent>().Count);
        }

        [Test]
        public void NotifyProgress_EmptyTarget_MatchesAll()
        {
            var t1 = TestHelpers.MakeAchievementTier(3);
            var data = TestHelpers.MakeAchievementData("a1", ConditionType.Kill, "", new[] { t1 });
            _system.RegisterAchievement(data);

            _system.NotifyProgress(ConditionType.Kill, "anything", 1);

            Assert.AreEqual(1, _eventBus.GetPublished<AchievementProgressEvent>().Count);
        }

        [Test]
        public void NotifyProgress_CrossingThreshold_PublishesCompletedEvent()
        {
            _system.RegisterAchievement(MakeAch("a1"));

            _system.NotifyProgress(ConditionType.Kill, "slime", 5);

            var completed = _eventBus.GetPublished<AchievementCompletedEvent>();
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(0, completed[0].Tier);
        }

        [Test]
        public void NotifyProgress_AlreadyCompleted_NoDoubleEvent()
        {
            _system.RegisterAchievement(MakeAch("a1"));
            _system.NotifyProgress(ConditionType.Kill, "slime", 5);
            _system.NotifyProgress(ConditionType.Kill, "slime", 1);

            Assert.AreEqual(1, _eventBus.GetPublished<AchievementCompletedEvent>().Count);
        }

        [Test]
        public void ClaimReward_NotCompleted_ReturnsFalse()
        {
            _system.RegisterAchievement(MakeAch("a1"));

            Assert.IsFalse(_system.ClaimReward("a1"));
        }

        [Test]
        public void ClaimReward_Completed_DispatchesRewardsAndAdvancesTier()
        {
            _system.RegisterAchievement(MakeAch("a1"));
            _system.NotifyProgress(ConditionType.Kill, "slime", 5);

            bool ok = _system.ClaimReward("a1");

            Assert.IsTrue(ok);

            Assert.AreEqual(1, _rewardHandler.Calls.Count);
            Assert.AreEqual(9001, _rewardHandler.Calls[0].RewardId);
            Assert.AreEqual(100, _rewardHandler.Calls[0].Amount);
            Assert.AreEqual("Achievement", _rewardHandler.Calls[0].Source);

            var claimedEvents = _eventBus.GetPublished<AchievementRewardClaimedEvent>();
            Assert.AreEqual(1, claimedEvents.Count);
            Assert.AreEqual(0, claimedEvents[0].Tier);
        }

        [Test]
        public void ClaimReward_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.ClaimReward("nonexistent"));
        }

        [Test]
        public void ProgressionFlow_AllTiersClaimed_EndsInRewardedAtMax()
        {
            _system.RegisterAchievement(MakeAch("a1"));

            _system.NotifyProgress(ConditionType.Kill, "slime", 150); // 전 티어 임계치 초과
            _system.ClaimReward("a1"); // tier 0 → 1
            _system.NotifyProgress(ConditionType.Kill, "slime", 0); // tier 1 재평가
            _system.ClaimReward("a1"); // tier 1 → 2
            _system.NotifyProgress(ConditionType.Kill, "slime", 0); // tier 2 재평가
            _system.ClaimReward("a1"); // tier 2 → 3 (max)

            Assert.AreEqual(AchievementState.Rewarded, _system.GetState("a1"));
            Assert.AreEqual(3, _rewardHandler.Calls.Count);

            // 이후 NotifyProgress 는 진행 이벤트 발행하지 않음
            int beforeProgressEvents = _eventBus.GetPublished<AchievementProgressEvent>().Count;
            _system.NotifyProgress(ConditionType.Kill, "slime", 1);
            Assert.AreEqual(beforeProgressEvents, _eventBus.GetPublished<AchievementProgressEvent>().Count);
        }

        [Test]
        public void GetAchievements_CategoryFilter_ReturnsOnlyMatching()
        {
            var combat = TestHelpers.MakeAchievementData("c", ConditionType.Kill, "", new[] { TestHelpers.MakeAchievementTier(1) }, AchievementCategory.Combat);
            var gacha = TestHelpers.MakeAchievementData("g", ConditionType.GachaPull, "", new[] { TestHelpers.MakeAchievementTier(1) }, AchievementCategory.Gacha);

            _system.RegisterAchievement(combat);
            _system.RegisterAchievement(gacha);

            var combats = _system.GetAchievements(AchievementCategory.Combat);
            var all = _system.GetAchievements(null);

            Assert.AreEqual(1, combats.Count);
            Assert.AreEqual("c", combats[0].AchievementId);
            Assert.AreEqual(2, all.Count);
        }

        [Test]
        public void GetProgress_UnknownAchievement_ReturnsZero()
        {
            Assert.AreEqual(0f, _system.GetProgress("nonexistent"));
        }

        [Test]
        public void SaveSystem_CalledOnProgress()
        {
            _system.RegisterAchievement(MakeAch("a1"));
            int before = _saveSystem.SaveCallCount;

            _system.NotifyProgress(ConditionType.Kill, "slime", 1);

            Assert.Greater(_saveSystem.SaveCallCount, before);
        }
    }
}
