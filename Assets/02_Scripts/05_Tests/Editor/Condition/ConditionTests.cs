using NUnit.Framework;

namespace PublicFramework.Tests.Condition
{
    public class ConditionTests
    {
        private ConditionData _data;
        private PublicFramework.Condition _condition;

        [SetUp]
        public void SetUp()
        {
            _data = TestHelpers.MakeConditionData("c1", ConditionType.Kill, "monster_slime", 5, "슬라임 5마리 처치");
            _condition = new PublicFramework.Condition(_data);
        }

        [Test]
        public void NewCondition_HasDataValues()
        {
            Assert.AreEqual("c1", _condition.ConditionId);
            Assert.AreEqual(ConditionType.Kill, _condition.ConditionType);
            Assert.AreEqual("monster_slime", _condition.TargetId);
            Assert.AreEqual(5, _condition.RequiredAmount);
            Assert.AreEqual(0, _condition.CurrentAmount);
            Assert.IsFalse(_condition.IsCompleted);
            Assert.AreEqual(0f, _condition.Progress);
        }

        [Test]
        public void NewCondition_WithInitialAmount_HasSeedValue()
        {
            var cond = new PublicFramework.Condition(_data, 2);
            Assert.AreEqual(2, cond.CurrentAmount);
            Assert.AreEqual(0.4f, cond.Progress, 0.0001f);
        }

        [Test]
        public void AddProgress_Positive_IncrementsCurrent()
        {
            _condition.AddProgress(2);
            Assert.AreEqual(2, _condition.CurrentAmount);
            Assert.AreEqual(0.4f, _condition.Progress, 0.0001f);
        }

        [Test]
        public void AddProgress_ZeroOrNegative_Ignored()
        {
            _condition.AddProgress(0);
            _condition.AddProgress(-3);
            Assert.AreEqual(0, _condition.CurrentAmount);
        }

        [Test]
        public void AddProgress_ExceedingRequired_ClampsToRequired()
        {
            _condition.AddProgress(100);
            Assert.AreEqual(5, _condition.CurrentAmount);
            Assert.IsTrue(_condition.IsCompleted);
            Assert.AreEqual(1f, _condition.Progress);
        }

        [Test]
        public void AddProgress_AfterCompletion_DoesNothing()
        {
            _condition.AddProgress(5);
            Assert.IsTrue(_condition.IsCompleted);

            _condition.AddProgress(3);
            Assert.AreEqual(5, _condition.CurrentAmount);
        }

        [Test]
        public void Reset_RestoresZero()
        {
            _condition.AddProgress(3);
            _condition.Reset();
            Assert.AreEqual(0, _condition.CurrentAmount);
            Assert.IsFalse(_condition.IsCompleted);
        }

        [Test]
        public void SetCurrentAmount_WithinRange_Sets()
        {
            _condition.SetCurrentAmount(3);
            Assert.AreEqual(3, _condition.CurrentAmount);
        }

        [Test]
        public void SetCurrentAmount_AboveRequired_Clamps()
        {
            _condition.SetCurrentAmount(999);
            Assert.AreEqual(5, _condition.CurrentAmount);
            Assert.IsTrue(_condition.IsCompleted);
        }

        [Test]
        public void ConditionProgress_InterfaceExposesDescription()
        {
            IConditionProgress progress = _condition;
            Assert.AreEqual("슬라임 5마리 처치", progress.Description);
            Assert.AreEqual(ConditionType.Kill, progress.ConditionType);
        }
    }
}
