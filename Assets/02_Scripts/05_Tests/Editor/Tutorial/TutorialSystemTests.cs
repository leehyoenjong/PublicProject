using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Tutorial
{
    public class TutorialSystemTests
    {
        private FakeEventBus _bus;
        private FakeSaveSystem _save;
        private FakeTutorialPresentation _present;
        private TutorialSystem _system;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _save = new FakeSaveSystem();
            _system = new TutorialSystem(_bus, _save);
            _present = new FakeTutorialPresentation();
            _system.SetPresentation(_present);
        }

        private TutorialData Register(
            string id,
            int stepCount = 1,
            TriggerType trigger = TriggerType.Manual,
            string triggerValue = "",
            int priority = 0,
            bool canSkip = true,
            string[] prereqs = null)
        {
            var steps = new TutorialStepData[stepCount];
            for (int i = 0; i < stepCount; i++)
            {
                steps[i] = TestHelpers.MakeTutorialStep(stepType: TutorialStepType.Dialog);
            }
            var data = TestHelpers.MakeTutorialData(id, steps, trigger, triggerValue, priority, canSkip, prereqs);
            _system.RegisterTutorial(data);
            return data;
        }

        // ---------- StartTutorial ----------

        [Test]
        public void StartTutorial_Registered_PublishesStartedEvent()
        {
            Register("t1", stepCount: 3);

            _system.StartTutorial("t1");

            var events = _bus.GetPublished<TutorialStartedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("t1", events[0].TutorialId);
            Assert.AreEqual(3, events[0].TotalSteps);
        }

        [Test]
        public void StartTutorial_Registered_SetsRunningState()
        {
            Register("t1");

            _system.StartTutorial("t1");

            Assert.IsTrue(_system.IsRunning);
            Assert.AreEqual("t1", _system.CurrentTutorialId);
            Assert.AreEqual(0, _system.CurrentStepIndex);
        }

        [Test]
        public void StartTutorial_Unknown_LogsErrorAndDoesNotStart()
        {
            LogAssert.Expect(LogType.Error, "[TutorialSystem] Tutorial not found: nope");

            _system.StartTutorial("nope");

            Assert.IsFalse(_system.IsRunning);
        }

        [Test]
        public void StartTutorial_AlreadyCompleted_DoesNotStart()
        {
            Register("t1");
            _system.CompleteTutorial("t1");

            _system.StartTutorial("t1");

            Assert.IsFalse(_system.IsRunning);
        }

        [Test]
        public void StartTutorial_AlreadyRunning_DoesNotStart()
        {
            Register("t1");
            Register("t2");
            _system.StartTutorial("t1");

            _system.StartTutorial("t2");

            Assert.AreEqual("t1", _system.CurrentTutorialId);
        }

        // ---------- Step 실행 ----------

        [Test]
        public void StartTutorial_PublishesStepChangedForFirstStep()
        {
            Register("t1");

            _system.StartTutorial("t1");

            var events = _bus.GetPublished<TutorialStepChangedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(0, events[0].StepIndex);
        }

        [Test]
        public void ExecuteStep_HighlightType_CallsShowHighlight()
        {
            var step = TestHelpers.MakeTutorialStep(stepType: TutorialStepType.Highlight);
            var data = TestHelpers.MakeTutorialData("t1", new[] { step });
            _system.RegisterTutorial(data);

            _system.StartTutorial("t1");

            Assert.AreEqual(1, _present.ShowHighlightCalls);
        }

        [Test]
        public void ExecuteStep_WithDialogText_CallsShowDialog()
        {
            var step = TestHelpers.MakeTutorialStep(dialogText: 42, dialogPosition: DialogPosition.Top);
            var data = TestHelpers.MakeTutorialData("t1", new[] { step });
            _system.RegisterTutorial(data);

            _system.StartTutorial("t1");

            Assert.AreEqual(1, _present.ShowDialogCalls);
            Assert.AreEqual(42, _present.LastDialogTextKey);
            Assert.AreEqual(DialogPosition.Top, _present.LastDialogPosition);
        }

        [Test]
        public void ExecuteStep_WithArrow_CallsShowArrow()
        {
            var step = TestHelpers.MakeTutorialStep(arrowDirection: ArrowDirection.Right);
            var data = TestHelpers.MakeTutorialData("t1", new[] { step });
            _system.RegisterTutorial(data);

            _system.StartTutorial("t1");

            Assert.AreEqual(1, _present.ShowArrowCalls);
            Assert.AreEqual(ArrowDirection.Right, _present.LastArrowDirection);
        }

        // ---------- NextStep ----------

        [Test]
        public void NextStep_AdvancesToNextStep()
        {
            Register("t1", stepCount: 3);
            _system.StartTutorial("t1");

            _system.NextStep();

            Assert.AreEqual(1, _system.CurrentStepIndex);
        }

        [Test]
        public void NextStep_LastStep_CompletesTutorial()
        {
            Register("t1", stepCount: 2);
            _system.StartTutorial("t1");
            _system.NextStep();

            _system.NextStep();

            Assert.IsFalse(_system.IsRunning);
            Assert.IsTrue(_system.IsTutorialCompleted("t1"));
        }

        [Test]
        public void NextStep_NotRunning_NoOp()
        {
            _system.NextStep();
            Assert.IsFalse(_system.IsRunning);
        }

        // ---------- Skip ----------

        [Test]
        public void SkipTutorial_CanSkip_PublishesSkippedAndCompletes()
        {
            Register("t1", stepCount: 3, canSkip: true);
            _system.StartTutorial("t1");
            _system.NextStep();

            _system.SkipTutorial();

            var skipped = _bus.GetPublished<TutorialSkippedEvent>();
            Assert.AreEqual(1, skipped.Count);
            Assert.AreEqual(1, skipped[0].SkippedAtStep);
            Assert.IsTrue(_system.IsTutorialCompleted("t1"));
        }

        [Test]
        public void SkipTutorial_CannotSkip_DoesNotSkip()
        {
            Register("t1", canSkip: false);
            _system.StartTutorial("t1");

            _system.SkipTutorial();

            Assert.IsTrue(_system.IsRunning);
            Assert.AreEqual(0, _bus.GetPublished<TutorialSkippedEvent>().Count);
        }

        // ---------- Complete ----------

        [Test]
        public void CompleteTutorial_AddsToCompletedList()
        {
            _system.CompleteTutorial("t1");

            Assert.IsTrue(_system.IsTutorialCompleted("t1"));
            CollectionAssert.Contains(_system.GetCompletedTutorials(), "t1");
        }

        [Test]
        public void CompleteTutorial_PublishesCompletedEvent()
        {
            _system.CompleteTutorial("t1");

            var events = _bus.GetPublished<TutorialCompletedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("t1", events[0].TutorialId);
        }

        [Test]
        public void CompleteTutorial_PersistsToSaveSystem()
        {
            int beforeSaves = _save.SaveCallCount;

            _system.CompleteTutorial("t1");

            Assert.Greater(_save.SaveCallCount, beforeSaves);
            Assert.IsTrue(_save.HasKey(0, "tutorial_completed"));
        }

        // ---------- CheckTriggers ----------

        [Test]
        public void CheckTriggers_MatchingTrigger_StartsTutorial()
        {
            Register("t1", trigger: TriggerType.LevelReach, triggerValue: "5");

            _system.CheckTriggers(TriggerType.LevelReach, "5");

            Assert.IsTrue(_system.IsRunning);
            Assert.AreEqual("t1", _system.CurrentTutorialId);
        }

        [Test]
        public void CheckTriggers_PrerequisitesNotMet_DoesNotStart()
        {
            Register("t1", trigger: TriggerType.Manual, prereqs: new[] { "missing_pre" });

            _system.CheckTriggers(TriggerType.Manual, null);

            Assert.IsFalse(_system.IsRunning);
        }

        [Test]
        public void CheckTriggers_HighestPriority_StartsFirst()
        {
            Register("low", trigger: TriggerType.Manual, priority: 1);
            Register("high", trigger: TriggerType.Manual, priority: 10);

            _system.CheckTriggers(TriggerType.Manual, null);

            Assert.AreEqual("high", _system.CurrentTutorialId);
        }

        // ---------- Constructor ----------

        [Test]
        public void Constructor_LoadsCompletedFromSaveSystem()
        {
            var save = new FakeSaveSystem();
            save.Save(0, "tutorial_completed", new List<string> { "preLoaded1", "preLoaded2" });

            var system = new TutorialSystem(new FakeEventBus(), save);

            Assert.IsTrue(system.IsTutorialCompleted("preLoaded1"));
            Assert.IsTrue(system.IsTutorialCompleted("preLoaded2"));
        }
    }
}
