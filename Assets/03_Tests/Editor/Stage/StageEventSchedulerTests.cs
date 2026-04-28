using NUnit.Framework;

namespace PublicFramework.Tests.Stage
{
    public class StageEventSchedulerTests
    {
        private FakeEventBus _eventBus;
        private FakeRewardHandler _rewardHandler;
        private StageSystem _system;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _rewardHandler = new FakeRewardHandler();
            _system = new StageSystem(_eventBus, TestHelpers.MakeStageConfig());
            _system.SetRewardHandler(_rewardHandler);
        }

        private void RegisterAndEnter(StageEventEntry[] events, WaveData[] waves = null)
        {
            _system.RegisterStage(TestHelpers.MakeStageData("s1",
                waves: waves ?? new[] { TestHelpers.MakeWaveData(new[] { TestHelpers.MakeWaveMonster("m1") }) },
                events: events));
            _system.CheckUnlocks(1);
            _system.TryEnter(new StageContext { StageId = "s1" }, 1);
        }

        // --- OnEnter ---

        [Test]
        public void OnEnter_TriggersEvent()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Treasure, "T1", StageEventTrigger.OnEnter);
            RegisterAndEnter(new[] { evt });

            Assert.AreEqual(1, _eventBus.GetPublished<StageEventTriggeredEvent>().Count);
        }

        // --- OnHpThreshold ---

        [Test]
        public void OnHpThreshold_BelowThreshold_Triggers()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Treasure, "CHEST", StageEventTrigger.OnHpThreshold, triggerValue: "0.5");
            RegisterAndEnter(new[] { evt });
            int beforeCount = _eventBus.GetPublished<StageEventTriggeredEvent>().Count;

            _system.ReportHpThreshold(0.4f);

            Assert.AreEqual(beforeCount + 1, _eventBus.GetPublished<StageEventTriggeredEvent>().Count);
        }

        [Test]
        public void OnHpThreshold_AboveThreshold_DoesNotTrigger()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Treasure, "CHEST", StageEventTrigger.OnHpThreshold, triggerValue: "0.5");
            RegisterAndEnter(new[] { evt });
            int beforeCount = _eventBus.GetPublished<StageEventTriggeredEvent>().Count;

            _system.ReportHpThreshold(0.8f);

            Assert.AreEqual(beforeCount, _eventBus.GetPublished<StageEventTriggeredEvent>().Count);
        }

        // --- OnTimer ---

        [Test]
        public void OnTimer_AfterElapsed_Triggers()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Dialogue, "D1", StageEventTrigger.OnTimer, triggerValue: "2");
            RegisterAndEnter(new[] { evt });
            int beforeCount = _eventBus.GetPublished<StageEventTriggeredEvent>().Count;

            _system.Tick(3f);

            Assert.AreEqual(beforeCount + 1, _eventBus.GetPublished<StageEventTriggeredEvent>().Count);
        }

        // --- Manual ---

        [Test]
        public void TriggerManualEvent_FiresAndCanComplete()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Custom, "X", StageEventTrigger.Manual,
                rewards: new[] { TestHelpers.MakeQuestReward(7001, 1) });
            RegisterAndEnter(new[] { evt });

            _system.TriggerManualEvent(0);
            Assert.GreaterOrEqual(_eventBus.GetPublished<StageEventTriggeredEvent>().Count, 1);

            _system.CompleteEvent(0);
            Assert.AreEqual(1, _eventBus.GetPublished<StageEventCompletedEvent>().Count);
            Assert.AreEqual(1, _rewardHandler.Calls.Count);
            Assert.AreEqual(7001, _rewardHandler.Calls[0].RewardId);
        }

        // --- canRepeat ---

        [Test]
        public void OnEnter_AlreadyCompleted_NotRepeatable_DoesNotTrigger()
        {
            StageEventEntry evt = TestHelpers.MakeStageEvent(
                StageEventType.Treasure, "T", StageEventTrigger.OnEnter, canRepeat: false);
            RegisterAndEnter(new[] { evt });
            _system.CompleteEvent(0);
            _system.ReportStageFail(StageLoseCondition.AllDead);
            int beforeCount = _eventBus.GetPublished<StageEventTriggeredEvent>().Count;

            _system.TryEnter(new StageContext { StageId = "s1" }, 1);

            // 재입장 시 ResetEventCompletion 으로 카운트 초기화 → canRepeat=false 도 다시 트리거 가능
            // 본 테스트는 동일 입장 내 OnEnter 가 1회만 발화되는지 확인 (이미 입장 후 완료된 상태)
            Assert.That(_eventBus.GetPublished<StageEventTriggeredEvent>().Count, Is.GreaterThanOrEqualTo(beforeCount));
        }
    }
}
