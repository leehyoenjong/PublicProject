using NUnit.Framework;

namespace PublicFramework.Tests.Condition
{
    public class ConditionGroupTests
    {
        private PublicFramework.Condition MakeCondition(string id, int required, int initial = 0)
        {
            ConditionData data = TestHelpers.MakeConditionData(id, ConditionType.Kill, "target", required);
            return new PublicFramework.Condition(data, initial);
        }

        [Test]
        public void Empty_AllType_IsCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            Assert.IsTrue(group.IsCompleted);
            Assert.AreEqual(1f, group.Progress);
        }

        [Test]
        public void All_AllComplete_IsCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(MakeCondition("a", 3, 3));
            group.AddCondition(MakeCondition("b", 2, 2));
            Assert.IsTrue(group.IsCompleted);
        }

        [Test]
        public void All_OnePending_IsNotCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(MakeCondition("a", 3, 3));
            group.AddCondition(MakeCondition("b", 2, 0));
            Assert.IsFalse(group.IsCompleted);
        }

        [Test]
        public void Any_OneComplete_IsCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.Any);
            group.AddCondition(MakeCondition("a", 3, 0));
            group.AddCondition(MakeCondition("b", 2, 2));
            Assert.IsTrue(group.IsCompleted);
        }

        [Test]
        public void Any_NoneComplete_IsNotCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.Any);
            group.AddCondition(MakeCondition("a", 3, 1));
            group.AddCondition(MakeCondition("b", 2, 1));
            Assert.IsFalse(group.IsCompleted);
        }

        [Test]
        public void Sequence_AllComplete_IsCompleted()
        {
            var group = new ConditionGroup(ConditionGroupType.Sequence);
            group.AddCondition(MakeCondition("a", 1, 1));
            group.AddCondition(MakeCondition("b", 1, 1));
            Assert.IsTrue(group.IsCompleted);
        }

        [Test]
        public void Sequence_GetActiveCondition_ReturnsFirstUnfinished()
        {
            var first = MakeCondition("a", 1, 1);
            var second = MakeCondition("b", 2, 0);
            var third = MakeCondition("c", 3, 0);

            var group = new ConditionGroup(ConditionGroupType.Sequence);
            group.AddCondition(first);
            group.AddCondition(second);
            group.AddCondition(third);

            Assert.AreSame(second, group.GetActiveCondition());
        }

        [Test]
        public void Sequence_GetActiveCondition_AllComplete_ReturnsNull()
        {
            var group = new ConditionGroup(ConditionGroupType.Sequence);
            group.AddCondition(MakeCondition("a", 1, 1));
            Assert.IsNull(group.GetActiveCondition());
        }

        [Test]
        public void All_GetActiveCondition_ReturnsNull()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(MakeCondition("a", 3, 1));
            Assert.IsNull(group.GetActiveCondition());
        }

        [Test]
        public void Progress_AverageOfChildren()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(MakeCondition("a", 4, 2)); // 0.5
            group.AddCondition(MakeCondition("b", 4, 4)); // 1.0
            Assert.AreEqual(0.75f, group.Progress, 0.0001f);
        }

        [Test]
        public void AddCondition_Null_Ignored()
        {
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(null);
            Assert.AreEqual(0, group.Conditions.Count);
        }

        [Test]
        public void ResetAll_ResetsEveryCondition()
        {
            var first = MakeCondition("a", 3, 3);
            var second = MakeCondition("b", 2, 2);
            var group = new ConditionGroup(ConditionGroupType.All);
            group.AddCondition(first);
            group.AddCondition(second);
            Assert.IsTrue(group.IsCompleted);

            group.ResetAll();
            Assert.AreEqual(0, first.CurrentAmount);
            Assert.AreEqual(0, second.CurrentAmount);
            Assert.IsFalse(group.IsCompleted);
        }

        [Test]
        public void GroupType_ExposedCorrectly()
        {
            Assert.AreEqual(ConditionGroupType.Any, new ConditionGroup(ConditionGroupType.Any).GroupType);
            Assert.AreEqual(ConditionGroupType.Sequence, new ConditionGroup(ConditionGroupType.Sequence).GroupType);
        }
    }
}
