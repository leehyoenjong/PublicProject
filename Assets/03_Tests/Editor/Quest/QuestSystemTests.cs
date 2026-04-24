using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Quest
{
    public class QuestSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeSaveSystem _saveSystem;
        private FakeRewardHandler _rewardHandler;
        private QuestSystem _system;

        private QuestData MakeKillQuest(string id, QuestType type, int requiredKills, int rewardId = 9001, int rewardAmount = 100, string[] prereqs = null)
        {
            var condition = TestHelpers.MakeConditionData($"{id}_c1", ConditionType.Kill, "monster", requiredKills);
            var reward = TestHelpers.MakeQuestReward(rewardId, rewardAmount);
            return TestHelpers.MakeQuestData(id, type, ConditionGroupType.All,
                new[] { condition }, new[] { reward }, prereqs);
        }

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _saveSystem = new FakeSaveSystem();
            _rewardHandler = new FakeRewardHandler();

            _system = new QuestSystem(_eventBus, _saveSystem);
            _system.SetRewardHandler(_rewardHandler);
        }

        // --- Register ---

        [Test]
        public void RegisterQuest_AddsQuestInLockedState()
        {
            QuestData data = MakeKillQuest("q1", QuestType.Main, 3);
            _system.RegisterQuest(data);

            IQuestInstance quest = _system.GetProgress("q1");
            Assert.IsNotNull(quest);
            Assert.AreEqual(QuestState.Locked, quest.State);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestRegisteredEvent>().Count);
        }

        [Test]
        public void RegisterQuest_Duplicate_Ignored()
        {
            QuestData data = MakeKillQuest("q1", QuestType.Main, 3);
            _system.RegisterQuest(data);
            _system.RegisterQuest(data);

            Assert.AreEqual(1, _eventBus.GetPublished<QuestRegisteredEvent>().Count);
        }

        [Test]
        public void RegisterQuest_Null_Ignored()
        {
            _system.RegisterQuest(null);
            Assert.AreEqual(0, _eventBus.GetPublished<QuestRegisteredEvent>().Count);
        }

        // --- CheckUnlocks ---

        [Test]
        public void CheckUnlocks_NoPrereqs_TransitionsToInProgress()
        {
            QuestData data = MakeKillQuest("q1", QuestType.Main, 3);
            _system.RegisterQuest(data);

            _system.CheckUnlocks();

            IQuestInstance quest = _system.GetProgress("q1");
            Assert.AreEqual(QuestState.InProgress, quest.State);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestUnlockedEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestAcceptedEvent>().Count);
        }

        [Test]
        public void CheckUnlocks_UnmetPrereq_StaysLocked()
        {
            _system.RegisterQuest(MakeKillQuest("pre", QuestType.Main, 1));
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3, prereqs: new[] { "pre" }));

            _system.CheckUnlocks();

            Assert.AreEqual(QuestState.Locked, _system.GetProgress("q1").State);
        }

        [Test]
        public void CheckUnlocks_SkipsAlreadyUnlocked()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            _system.CheckUnlocks();
            _system.CheckUnlocks();

            Assert.AreEqual(1, _eventBus.GetPublished<QuestUnlockedEvent>().Count);
        }

        // --- AcceptQuest ---

        [Test]
        public void AcceptQuest_NotAvailable_ReturnsFalse()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            bool accepted = _system.AcceptQuest("q1");

            Assert.IsFalse(accepted);
        }

        [Test]
        public void AcceptQuest_Available_TransitionsToInProgress()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            ((QuestInstance)_system.GetProgress("q1")).SetState(QuestState.Available);

            bool accepted = _system.AcceptQuest("q1");

            Assert.IsTrue(accepted);
            Assert.AreEqual(QuestState.InProgress, _system.GetProgress("q1").State);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestAcceptedEvent>().Count);
        }

        [Test]
        public void AcceptQuest_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.AcceptQuest("nonexistent"));
        }

        // --- NotifyConditionProgress ---

        [Test]
        public void NotifyConditionProgress_AllGroup_UpdatesMatchingCondition()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            _system.CheckUnlocks();
            _eventBus.Clear();

            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);

            IQuestInstance quest = _system.GetProgress("q1");
            Assert.AreEqual(1f / 3f, quest.Progress, 0.0001f);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestProgressEvent>().Count);
        }

        [Test]
        public void NotifyConditionProgress_Completing_PublishesCompletedEvent()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 2));
            _system.CheckUnlocks();

            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 2);

            Assert.AreEqual(QuestState.Completed, _system.GetProgress("q1").State);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestCompletedEvent>().Count);
        }

        [Test]
        public void NotifyConditionProgress_UnmatchedTarget_Ignored()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            _system.CheckUnlocks();

            _system.NotifyConditionProgress(ConditionType.Kill, "other", 1);

            Assert.AreEqual(0f, _system.GetProgress("q1").Progress);
        }

        [Test]
        public void NotifyConditionProgress_NotInProgress_Ignored()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            // state is Locked — no CheckUnlocks

            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);

            Assert.AreEqual(0f, _system.GetProgress("q1").Progress);
        }

        [Test]
        public void NotifyConditionProgress_Sequence_OnlyActiveProgresses()
        {
            var c1 = TestHelpers.MakeConditionData("c1", ConditionType.Kill, "slime", 1);
            var c2 = TestHelpers.MakeConditionData("c2", ConditionType.Kill, "slime", 3);
            QuestData data = TestHelpers.MakeQuestData("q1", QuestType.Main, ConditionGroupType.Sequence,
                new[] { c1, c2 });
            _system.RegisterQuest(data);
            _system.CheckUnlocks();

            _system.NotifyConditionProgress(ConditionType.Kill, "slime", 1);

            var instance = (QuestInstance)_system.GetProgress("q1");
            Assert.AreEqual(1, instance.ConditionGroup.Conditions[0].CurrentAmount);
            Assert.AreEqual(0, instance.ConditionGroup.Conditions[1].CurrentAmount);

            _system.NotifyConditionProgress(ConditionType.Kill, "slime", 2);

            Assert.AreEqual(1, instance.ConditionGroup.Conditions[0].CurrentAmount);
            Assert.AreEqual(2, instance.ConditionGroup.Conditions[1].CurrentAmount);
        }

        // --- ClaimReward ---

        [Test]
        public void ClaimReward_NotCompleted_ReturnsFalse()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            _system.CheckUnlocks();

            Assert.IsFalse(_system.ClaimReward("q1"));
        }

        [Test]
        public void ClaimReward_Completed_TransitionsToRewardedAndDispatches()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 1, rewardId: 5001, rewardAmount: 50));
            _system.CheckUnlocks();
            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);

            bool claimed = _system.ClaimReward("q1");

            Assert.IsTrue(claimed);
            Assert.AreEqual(QuestState.Rewarded, _system.GetProgress("q1").State);

            Assert.AreEqual(1, _rewardHandler.Calls.Count);
            Assert.AreEqual(5001, _rewardHandler.Calls[0].RewardId);
            Assert.AreEqual(50, _rewardHandler.Calls[0].Amount);
            Assert.AreEqual("Quest", _rewardHandler.Calls[0].Source);

            Assert.AreEqual(1, _eventBus.GetPublished<QuestRewardClaimedEvent>().Count);
        }

        [Test]
        public void ClaimReward_AlreadyRewarded_ReturnsFalse()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 1));
            _system.CheckUnlocks();
            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);
            _system.ClaimReward("q1");

            Assert.IsFalse(_system.ClaimReward("q1"));
        }

        // --- Abandon ---

        [Test]
        public void AbandonQuest_InProgress_ResetsProgressAndKeepsInProgress()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            _system.CheckUnlocks();
            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 2);

            bool abandoned = _system.AbandonQuest("q1");

            Assert.IsTrue(abandoned);
            Assert.AreEqual(0f, _system.GetProgress("q1").Progress);
            Assert.AreEqual(1, _eventBus.GetPublished<QuestAbandonedEvent>().Count);
        }

        [Test]
        public void AbandonQuest_NotInProgress_ReturnsFalse()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 3));
            Assert.IsFalse(_system.AbandonQuest("q1"));
        }

        // --- ResetDaily / ResetWeekly ---

        [Test]
        public void ResetDaily_OnlyAffectsDailyQuests()
        {
            _system.RegisterQuest(MakeKillQuest("daily", QuestType.Daily, 1));
            _system.RegisterQuest(MakeKillQuest("main", QuestType.Main, 1));
            _system.CheckUnlocks();

            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);
            Assert.AreEqual(QuestState.Completed, _system.GetProgress("daily").State);
            Assert.AreEqual(QuestState.Completed, _system.GetProgress("main").State);

            _system.ResetDaily();

            Assert.AreEqual(QuestState.InProgress, _system.GetProgress("daily").State);
            Assert.AreEqual(0f, _system.GetProgress("daily").Progress);
            Assert.AreEqual(QuestState.Completed, _system.GetProgress("main").State);

            var resetEvents = _eventBus.GetPublished<QuestResetEvent>();
            Assert.AreEqual(1, resetEvents.Count);
            Assert.AreEqual(QuestType.Daily, resetEvents[0].QuestType);
        }

        [Test]
        public void ResetWeekly_OnlyAffectsWeeklyQuests()
        {
            _system.RegisterQuest(MakeKillQuest("weekly", QuestType.Weekly, 1));
            _system.CheckUnlocks();
            _system.NotifyConditionProgress(ConditionType.Kill, "monster", 1);

            _system.ResetWeekly();

            Assert.AreEqual(QuestState.InProgress, _system.GetProgress("weekly").State);
            Assert.AreEqual(QuestType.Weekly, _eventBus.GetPublished<QuestResetEvent>()[0].QuestType);
        }

        // --- GetQuests filters ---

        [Test]
        public void GetQuests_StateFilter_ReturnsOnlyMatching()
        {
            _system.RegisterQuest(MakeKillQuest("a", QuestType.Main, 1));
            _system.RegisterQuest(MakeKillQuest("b", QuestType.Main, 1));
            _system.CheckUnlocks();

            var inProgress = _system.GetQuests(QuestState.InProgress, null);
            var locked = _system.GetQuests(QuestState.Locked, null);

            Assert.AreEqual(2, inProgress.Count);
            Assert.AreEqual(0, locked.Count);
        }

        [Test]
        public void GetQuests_TypeFilter_ReturnsOnlyMatching()
        {
            _system.RegisterQuest(MakeKillQuest("main", QuestType.Main, 1));
            _system.RegisterQuest(MakeKillQuest("daily", QuestType.Daily, 1));

            var mains = _system.GetQuests(null, QuestType.Main);
            Assert.AreEqual(1, mains.Count);
            Assert.AreEqual("main", mains[0].QuestId);
        }

        // --- Persistence ---

        [Test]
        public void SaveSystem_CalledOnStateChange()
        {
            _system.RegisterQuest(MakeKillQuest("q1", QuestType.Main, 1));
            int before = _saveSystem.SaveCallCount;

            _system.CheckUnlocks();

            Assert.Greater(_saveSystem.SaveCallCount, before);
        }
    }
}
