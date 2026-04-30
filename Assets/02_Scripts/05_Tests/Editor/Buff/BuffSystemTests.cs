using NUnit.Framework;

namespace PublicFramework.Tests.Buff
{
    public class BuffSystemTests
    {
        private const string TARGET = "target1";
        private const string SOURCE = "source1";

        private FakeStatSystem _stats;
        private FakeEventBus _bus;
        private BuffSystem _system;

        [SetUp]
        public void SetUp()
        {
            _stats = new FakeStatSystem();
            _bus = new FakeEventBus();
            _system = new BuffSystem(_stats, _bus);
        }

        private static PassiveStat[] OnePassive(float value = 10f)
        {
            return new[] { new PassiveStat(StatType.Attack, StatLayer.Flat, value) };
        }

        // ---------- Apply: 기본 ----------

        [Test]
        public void AddBuff_NewBuff_Succeeds()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1");

            BuffResult result = _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("buf1", result.BuffId);
            Assert.AreEqual(1, result.CurrentStack);
            Assert.IsTrue(_system.HasBuff(TARGET, "buf1"));
        }

        [Test]
        public void AddBuff_NewBuff_PublishesAppliedEvent()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", duration: 5f);

            _system.AddBuff(TARGET, data, SOURCE);

            var events = _bus.GetPublished<BuffAppliedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("buf1", events[0].BuffId);
            Assert.AreEqual(TARGET, events[0].TargetId);
            Assert.AreEqual(1, events[0].StackCount);
            Assert.AreEqual(5f, events[0].Duration, 0.001f);
        }

        [Test]
        public void AddBuff_NewBuff_AddsModifiersToContainer()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", targetStats: OnePassive(15f));

            _system.AddBuff(TARGET, data, SOURCE);

            FakeStatContainer container = _stats.GetOrCreate(TARGET);
            Assert.AreEqual(1, container.AddModifierCalls);
            Assert.AreEqual(1, container.Modifiers.Count);
            Assert.AreEqual(StatType.Attack, container.Modifiers[0].TargetStat);
            Assert.AreEqual(15f, container.Modifiers[0].Value, 0.001f);
        }

        [Test]
        public void AddBuff_NullBuffData_Fails()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "[버프] BuffData가 null임.");

            BuffResult result = _system.AddBuff(TARGET, null, SOURCE);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("BuffData is null", result.FailReason);
        }

        [Test]
        public void AddBuff_EmptyTargetId_Fails()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "[버프] targetId가 null 또는 빈 값임.");
            BuffData data = TestHelpers.MakeBuffData("buf1");

            BuffResult result = _system.AddBuff("", data, SOURCE);

            Assert.IsFalse(result.Success);
        }

        // ---------- Apply: 면역 ----------

        [Test]
        public void AddImmunity_BuffId_BlocksBuff()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1");
            _system.AddImmunity(TARGET, "buf1");

            BuffResult result = _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Immune", result.FailReason);
            Assert.IsFalse(_system.HasBuff(TARGET, "buf1"));
            Assert.AreEqual(1, _bus.GetPublished<BuffImmuneEvent>().Count);
        }

        [Test]
        public void AddImmunity_Category_BlocksBuff()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", category: BuffCategory.Negative);
            _system.AddImmunity(TARGET, "Negative");

            BuffResult result = _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1, _bus.GetPublished<BuffImmuneEvent>().Count);
        }

        [Test]
        public void RemoveImmunity_AllowsBuffAgain()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1");
            _system.AddImmunity(TARGET, "buf1");
            _system.RemoveImmunity(TARGET, "buf1");

            BuffResult result = _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsTrue(result.Success);
        }

        // ---------- Apply: 스택 정책 ----------

        [Test]
        public void AddBuff_StackNone_ExistingBuff_PublishesRefreshed()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", stackPolicy: StackPolicy.None,
                refreshPolicy: RefreshPolicy.Reset, duration: 10f);

            _system.AddBuff(TARGET, data, SOURCE);
            _system.AddBuff(TARGET, data, SOURCE);

            Assert.AreEqual(1, _system.GetBuffs(TARGET).Count);
            Assert.AreEqual(1, _bus.GetPublished<BuffRefreshedEvent>().Count);
        }

        [Test]
        public void AddBuff_StackDuration_ExtendsRemainingDuration()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                stackPolicy: StackPolicy.Duration, duration: 10f);

            _system.AddBuff(TARGET, data, SOURCE);
            _system.AddBuff(TARGET, data, SOURCE);

            IBuffInstance inst = _system.GetBuffs(TARGET)[0];
            Assert.AreEqual(20f, inst.RemainingDuration, 0.001f);
        }

        [Test]
        public void AddBuff_StackIntensity_IncreasesStack()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                stackPolicy: StackPolicy.Intensity, maxStack: 3, targetStats: OnePassive());

            _system.AddBuff(TARGET, data, SOURCE);
            _system.AddBuff(TARGET, data, SOURCE);

            Assert.AreEqual(2, _system.GetStackCount(TARGET, "buf1"));

            var stackEvents = _bus.GetPublished<BuffStackChangedEvent>();
            Assert.AreEqual(1, stackEvents.Count);
            Assert.AreEqual(1, stackEvents[0].OldStack);
            Assert.AreEqual(2, stackEvents[0].NewStack);
        }

        [Test]
        public void AddBuff_StackIntensity_AtMaxStack_DoesNotIncrease()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                stackPolicy: StackPolicy.Intensity, maxStack: 1);

            _system.AddBuff(TARGET, data, SOURCE);
            BuffResult second = _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsTrue(second.Success);
            Assert.AreEqual("MaxStack", second.FailReason);
            Assert.AreEqual(1, _system.GetStackCount(TARGET, "buf1"));
        }

        [Test]
        public void AddBuff_StackIndependent_CreatesSecondInstance()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                stackPolicy: StackPolicy.Independent);

            _system.AddBuff(TARGET, data, SOURCE);
            _system.AddBuff(TARGET, data, SOURCE);

            Assert.AreEqual(2, _system.GetBuffs(TARGET).Count);
        }

        // ---------- Tick (시간 기반) ----------

        [Test]
        public void Tick_TimedBuff_DecrementsDuration()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", duration: 10f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.Tick(3f);

            Assert.AreEqual(7f, _system.GetBuffs(TARGET)[0].RemainingDuration, 0.001f);
        }

        [Test]
        public void Tick_TimedBuff_OverDuration_Expires()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", duration: 5f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.Tick(6f);

            Assert.AreEqual(0, _system.GetBuffs(TARGET).Count);
        }

        [Test]
        public void Tick_TimedBuff_Expiry_PublishesExpiredAndRemovedEvents()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", duration: 5f);
            _system.AddBuff(TARGET, data, SOURCE);
            _bus.Clear();

            _system.Tick(6f);

            Assert.AreEqual(1, _bus.GetPublished<BuffExpiredEvent>().Count);
            Assert.AreEqual(1, _bus.GetPublished<BuffRemovedEvent>().Count);
        }

        [Test]
        public void Tick_PermanentBuff_DoesNotExpire()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                durationType: DurationType.Permanent, duration: 5f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.Tick(100f);

            Assert.AreEqual(1, _system.GetBuffs(TARGET).Count);
        }

        // ---------- ProcessTurn ----------

        [Test]
        public void ProcessTurn_TurnBasedBuff_DecrementsTurns()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                durationType: DurationType.TurnBased, duration: 3f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.ProcessTurn(TARGET);

            Assert.AreEqual(1, _system.GetBuffs(TARGET).Count);
            Assert.AreEqual(2f, _system.GetBuffs(TARGET)[0].RemainingDuration, 0.001f);
        }

        [Test]
        public void ProcessTurn_TurnBasedBuff_ReachesZero_Expires()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                durationType: DurationType.TurnBased, duration: 2f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.ProcessTurn(TARGET);
            _system.ProcessTurn(TARGET);

            Assert.AreEqual(0, _system.GetBuffs(TARGET).Count);
            Assert.AreEqual(1, _bus.GetPublished<BuffExpiredEvent>().Count);
        }

        [Test]
        public void ProcessTurn_TimedBuff_DoesNotDecrement()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1",
                durationType: DurationType.Timed, duration: 5f);
            _system.AddBuff(TARGET, data, SOURCE);

            _system.ProcessTurn(TARGET);

            Assert.AreEqual(5f, _system.GetBuffs(TARGET)[0].RemainingDuration, 0.001f);
        }

        // ---------- Remove ----------

        [Test]
        public void RemoveBuff_Existing_RemovesAndPublishesRemoved()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1");
            _system.AddBuff(TARGET, data, SOURCE);

            bool ok = _system.RemoveBuff(TARGET, "buf1");

            Assert.IsTrue(ok);
            Assert.IsFalse(_system.HasBuff(TARGET, "buf1"));
            Assert.AreEqual(1, _bus.GetPublished<BuffRemovedEvent>().Count);
        }

        [Test]
        public void RemoveBuff_Missing_ReturnsFalse()
        {
            Assert.IsFalse(_system.RemoveBuff(TARGET, "nobuff"));
        }

        [Test]
        public void RemoveAllBuffs_PreservesUndispellable()
        {
            BuffData normal = TestHelpers.MakeBuffData("buf1");
            BuffData sticky = TestHelpers.MakeBuffData("buf2", isUndispellable: true);
            _system.AddBuff(TARGET, normal, SOURCE);
            _system.AddBuff(TARGET, sticky, SOURCE);

            int removed = _system.RemoveAllBuffs(TARGET);

            Assert.AreEqual(1, removed);
            Assert.IsFalse(_system.HasBuff(TARGET, "buf1"));
            Assert.IsTrue(_system.HasBuff(TARGET, "buf2"));
        }

        [Test]
        public void RemoveAllBuffs_CategoryFilter_RemovesOnlyMatching()
        {
            BuffData pos = TestHelpers.MakeBuffData("buf1", category: BuffCategory.Positive);
            BuffData neg = TestHelpers.MakeBuffData("buf2", category: BuffCategory.Negative);
            _system.AddBuff(TARGET, pos, SOURCE);
            _system.AddBuff(TARGET, neg, SOURCE);

            int removed = _system.RemoveAllBuffs(TARGET, BuffCategory.Negative);

            Assert.AreEqual(1, removed);
            Assert.IsTrue(_system.HasBuff(TARGET, "buf1"));
            Assert.IsFalse(_system.HasBuff(TARGET, "buf2"));
        }

        [Test]
        public void RemoveBuff_InvokesContainerRemoveFromSource()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1", targetStats: OnePassive());
            _system.AddBuff(TARGET, data, SOURCE);
            FakeStatContainer container = _stats.GetOrCreate(TARGET);

            _system.RemoveBuff(TARGET, "buf1");

            Assert.GreaterOrEqual(container.RemoveFromSourceCalls, 1);
            Assert.AreEqual(0, container.Modifiers.Count);
        }

        // ---------- Query ----------

        [Test]
        public void HasBuff_WhenApplied_ReturnsTrue()
        {
            BuffData data = TestHelpers.MakeBuffData("buf1");
            _system.AddBuff(TARGET, data, SOURCE);

            Assert.IsTrue(_system.HasBuff(TARGET, "buf1"));
            Assert.IsFalse(_system.HasBuff(TARGET, "nobuff"));
        }

        [Test]
        public void GetStackCount_NoBuff_ReturnsZero()
        {
            Assert.AreEqual(0, _system.GetStackCount(TARGET, "anything"));
        }

        [Test]
        public void GetBuffs_EmptyTarget_ReturnsEmpty()
        {
            Assert.AreEqual(0, _system.GetBuffs("unknown").Count);
        }
    }
}
