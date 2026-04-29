using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Stage
{
    public class StageSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeRewardHandler _rewardHandler;
        private StageConfig _config;
        private StageSystem _system;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _rewardHandler = new FakeRewardHandler();
            _config = TestHelpers.MakeStageConfig();
            _system = new StageSystem(_eventBus, _config);
            _system.SetRewardHandler(_rewardHandler);
        }

        // --- Register ---

        [Test]
        public void RegisterStage_AddsInstanceInLockedState()
        {
            StageData stage = TestHelpers.MakeStageData("s1");
            _system.RegisterStage(stage);

            StageInstance inst = _system.GetInstance("s1");
            Assert.IsNotNull(inst);
            Assert.AreEqual(StageState.Locked, inst.State);
            Assert.AreEqual(1, _eventBus.GetPublished<StageRegisteredEvent>().Count);
        }

        [Test]
        public void RegisterStage_Duplicate_Ignored()
        {
            StageData stage = TestHelpers.MakeStageData("s1");
            _system.RegisterStage(stage);
            _system.RegisterStage(stage);

            Assert.AreEqual(1, _eventBus.GetPublished<StageRegisteredEvent>().Count);
        }

        [Test]
        public void RegisterStage_Null_Ignored()
        {
            _system.RegisterStage(null);
            Assert.AreEqual(0, _eventBus.GetPublished<StageRegisteredEvent>().Count);
        }

        // --- CheckUnlocks ---

        [Test]
        public void CheckUnlocks_NoPrereqs_TransitionsToAvailable()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1"));
            _system.CheckUnlocks(playerLevel: 1);

            Assert.AreEqual(StageState.Available, _system.GetInstance("s1").State);
            Assert.AreEqual(1, _eventBus.GetPublished<StageUnlockedEvent>().Count);
        }

        [Test]
        public void CheckUnlocks_UnmetPrereq_StaysLocked()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("pre"));
            _system.RegisterStage(TestHelpers.MakeStageData("s1", prereqStageIds: new[] { "pre" }));
            _system.CheckUnlocks(playerLevel: 1);

            Assert.AreEqual(StageState.Locked, _system.GetInstance("s1").State);
        }

        [Test]
        public void CheckUnlocks_RequiredLevelNotMet_StaysLocked()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1", requiredLevel: 10));
            _system.CheckUnlocks(playerLevel: 5);

            Assert.AreEqual(StageState.Locked, _system.GetInstance("s1").State);
        }

        // --- CanEnter / TryEnter ---

        [Test]
        public void CanEnter_Locked_ReturnsFalse()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1"));
            Assert.IsFalse(_system.CanEnter("s1", playerLevel: 1));
        }

        [Test]
        public void CanEnter_LevelTooLow_ReturnsFalse()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1", requiredLevel: 10));
            _system.CheckUnlocks(playerLevel: 10);
            Assert.IsFalse(_system.CanEnter("s1", playerLevel: 5));
        }

        [Test]
        public void TryEnter_Available_PublishesEnteredAndStartsFirstWave()
        {
            WaveData wave = TestHelpers.MakeWaveData(new[] { TestHelpers.MakeWaveMonster("m1") });
            _system.RegisterStage(TestHelpers.MakeStageData("s1", waves: new[] { wave }));
            _system.CheckUnlocks(playerLevel: 1);

            var ctx = new StageContext { StageId = "s1" };
            bool ok = _system.TryEnter(ctx, playerLevel: 1);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, _eventBus.GetPublished<StageEnteredEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<WaveStartedEvent>().Count);
            Assert.AreEqual(StageState.InProgress, _system.GetInstance("s1").State);
        }

        // --- Wave 진행 ---

        [Test]
        public void ReportWaveCleared_AdvancesToNextWave_PublishesEvents()
        {
            WaveData w1 = TestHelpers.MakeWaveData(new[] { TestHelpers.MakeWaveMonster("m1") });
            WaveData w2 = TestHelpers.MakeWaveData(new[] { TestHelpers.MakeWaveMonster("m2") });
            _system.RegisterStage(TestHelpers.MakeStageData("s1", waves: new[] { w1, w2 }));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);

            _system.ReportWaveCleared();

            Assert.AreEqual(1, _eventBus.GetPublished<WaveClearedEvent>().Count);
            Assert.AreEqual(2, _eventBus.GetPublished<WaveStartedEvent>().Count);
            Assert.AreEqual(1, _system.GetInstance("s1").CurrentWaveIndex);
        }

        [Test]
        public void ReportWaveCleared_LastWave_TriggersOnAllClear()
        {
            WaveData w1 = TestHelpers.MakeWaveData(new[] { TestHelpers.MakeWaveMonster("m1") });
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.PetRescue, "PET_FOX", StageEventTrigger.OnAllClear);
            _system.RegisterStage(TestHelpers.MakeStageData("s1", waves: new[] { w1 }, events: new[] { evt }));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);

            _system.ReportWaveCleared();

            Assert.AreEqual(1, _eventBus.GetPublished<StageEventTriggeredEvent>().Count);
        }

        // --- 클리어 + 보상 ---

        [Test]
        public void ReportStageWin_FirstClear_AwardsFirstAndRepeatRewards()
        {
            QuestReward first = TestHelpers.MakeQuestReward(101, 1);
            QuestReward repeat = TestHelpers.MakeQuestReward(102, 5);
            _system.RegisterStage(TestHelpers.MakeStageData("s1",
                firstClearRewards: new[] { first },
                repeatRewards: new[] { repeat }));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);

            _system.ReportStageWin(starsAchieved: 3);

            Assert.AreEqual(2, _rewardHandler.Calls.Count);
            Assert.AreEqual(101, _rewardHandler.Calls[0].RewardId);
            Assert.AreEqual(102, _rewardHandler.Calls[1].RewardId);

            StageInstance inst = _system.GetInstance("s1");
            Assert.AreEqual(StageState.Cleared, inst.State);
            Assert.AreEqual(1, inst.ClearCount);
            Assert.AreEqual(3, inst.BestStars);
            Assert.AreEqual(1, _eventBus.GetPublished<StageClearedEvent>().Count);
        }

        [Test]
        public void ReportStageWin_SecondClear_OnlyRepeatRewards()
        {
            QuestReward first = TestHelpers.MakeQuestReward(101, 1);
            QuestReward repeat = TestHelpers.MakeQuestReward(102, 5);
            _system.RegisterStage(TestHelpers.MakeStageData("s1",
                firstClearRewards: new[] { first },
                repeatRewards: new[] { repeat }));
            _system.CheckUnlocks(playerLevel: 1);

            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);
            _system.ReportStageWin(2);
            _rewardHandler.Clear();

            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);
            _system.ReportStageWin(3);

            Assert.AreEqual(1, _rewardHandler.Calls.Count);
            Assert.AreEqual(102, _rewardHandler.Calls[0].RewardId);
            Assert.AreEqual(3, _system.GetInstance("s1").BestStars);
        }

        [Test]
        public void ReportStageFail_RestoresAvailable_PublishesFailEvent()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1"));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);

            _system.ReportStageFail(StageLoseCondition.AllDead);

            Assert.AreEqual(StageState.Available, _system.GetInstance("s1").State);
            Assert.AreEqual(1, _eventBus.GetPublished<StageFailedEvent>().Count);
        }

        // --- 소탕 ---

        [Test]
        public void TrySweep_NotEnabled_Fails()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1", sweepEnabled: false));
            _system.CheckUnlocks(playerLevel: 1);

            bool ok = _system.TrySweep("s1", playerLevel: 1, times: 3);
            Assert.IsFalse(ok);
        }

        [Test]
        public void TrySweep_BeforeFirstClear_Fails()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1", sweepEnabled: true));
            _system.CheckUnlocks(playerLevel: 1);

            bool ok = _system.TrySweep("s1", playerLevel: 1, times: 3);
            Assert.IsFalse(ok);
        }

        [Test]
        public void TrySweep_AfterFirstClear_AwardsSweepRewardsTimes()
        {
            QuestReward sweep = TestHelpers.MakeQuestReward(901, 10);
            _system.RegisterStage(TestHelpers.MakeStageData("s1",
                sweepEnabled: true,
                sweepRewards: new[] { sweep },
                firstClearRewards: new[] { TestHelpers.MakeQuestReward(101, 1) }));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);
            _system.ReportStageWin(2);
            _rewardHandler.Clear();

            bool ok = _system.TrySweep("s1", playerLevel: 1, times: 3);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, _rewardHandler.Calls.Count);
            for (int i = 0; i < 3; i++) Assert.AreEqual(901, _rewardHandler.Calls[i].RewardId);
            Assert.AreEqual(1, _eventBus.GetPublished<StageSweptEvent>().Count);
            Assert.AreEqual(4, _system.GetInstance("s1").ClearCount);
        }

        // --- Tick / Timeout ---

        [Test]
        public void Tick_TimeLimitExceeded_FailsWithTimeout()
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1",
                timeLimitSeconds: 5f, loseCondition: StageLoseCondition.Timeout));
            _system.CheckUnlocks(playerLevel: 1);
            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);

            _system.Tick(6f);

            Assert.AreEqual(1, _eventBus.GetPublished<StageFailedEvent>().Count);
            Assert.AreEqual(StageState.Available, _system.GetInstance("s1").State);
        }

        // --- Chapter 완주 ---

        [Test]
        public void ReportStageWin_AllStagesCleared_PublishesChapterCompleted()
        {
            QuestReward chapterReward = TestHelpers.MakeQuestReward(999, 1);
            _system.RegisterChapter(TestHelpers.MakeChapterData("ch1", completeRewards: new[] { chapterReward }));
            _system.RegisterStage(TestHelpers.MakeStageData("s1", chapterId: "ch1"));
            _system.RegisterStage(TestHelpers.MakeStageData("s2", chapterId: "ch1"));
            _system.CheckUnlocks(playerLevel: 1);

            _system.TryEnter(new StageContext { StageId = "s1" }, playerLevel: 1);
            _system.ReportStageWin(1);
            Assert.AreEqual(0, _eventBus.GetPublished<ChapterCompletedEvent>().Count);

            _system.TryEnter(new StageContext { StageId = "s2" }, playerLevel: 1);
            _system.ReportStageWin(1);

            Assert.AreEqual(1, _eventBus.GetPublished<ChapterCompletedEvent>().Count);
            Assert.IsTrue(_system.IsChapterCompleted("ch1"));
            // 챕터 보상이 마지막 호출
            Assert.AreEqual(999, _rewardHandler.Calls[_rewardHandler.Calls.Count - 1].RewardId);
        }
    }
}
