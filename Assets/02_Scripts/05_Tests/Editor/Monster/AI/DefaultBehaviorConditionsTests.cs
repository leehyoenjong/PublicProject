using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Monster.AI
{
    public class DefaultBehaviorConditionsTests
    {
        private BehaviorContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _ctx = new BehaviorContext();
        }

        // --- HpBelowCondition ---

        [Test]
        public void HpBelow_NullStats_ReturnsFailure()
        {
            var cond = new HpBelowCondition();
            _ctx.SelfStats = null;
            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "0.5", "", ""));
        }

        [Test]
        public void HpBelow_InvalidThreshold_ReturnsFailure()
        {
            var cond = new HpBelowCondition();
            _ctx.SelfStats = new FakeStatContainer();
            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "abc", "", ""));
        }

        [Test]
        public void HpBelow_ZeroMaxHp_ReturnsFailure()
        {
            var cond = new HpBelowCondition();
            var stats = new FakeStatContainer();
            stats.SetCurrentHP(50f);
            stats.SetFinalValue(StatType.HP, 0f);
            _ctx.SelfStats = stats;

            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "0.5", "", ""));
        }

        [Test]
        public void HpBelow_RatioBelowThreshold_ReturnsSuccess()
        {
            var cond = new HpBelowCondition();
            var stats = new FakeStatContainer();
            stats.SetCurrentHP(30f);
            stats.SetFinalValue(StatType.HP, 100f);
            _ctx.SelfStats = stats;

            Assert.AreEqual(BehaviorNodeStatus.Success, cond.Tick(_ctx, "0.5", "", ""));
        }

        [Test]
        public void HpBelow_RatioAboveThreshold_ReturnsFailure()
        {
            var cond = new HpBelowCondition();
            var stats = new FakeStatContainer();
            stats.SetCurrentHP(70f);
            stats.SetFinalValue(StatType.HP, 100f);
            _ctx.SelfStats = stats;

            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "0.5", "", ""));
        }

        // --- TargetInRangeCondition ---

        [Test]
        public void TargetInRange_NullSelfOrTarget_ReturnsFailure()
        {
            var cond = new TargetInRangeCondition();
            _ctx.Self = null;
            _ctx.Target = null;
            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "5", "", ""));
        }

        [Test]
        public void TargetInRange_WithinRange_ReturnsSuccess()
        {
            var cond = new TargetInRangeCondition();
            var info = TestHelpers.MakeMonsterInfo("MID_S");
            var inst = new MonsterInstance("S", info, new FakeStatContainer());
            inst.SetPosition(Vector3.zero);

            _ctx.Self = inst;
            _ctx.Target = TestHelpers.MakeMonsterInfo("MID_T");
            _ctx.TargetPosition = new Vector3(3f, 0f, 0f);

            Assert.AreEqual(BehaviorNodeStatus.Success, cond.Tick(_ctx, "5", "", ""));
        }

        [Test]
        public void TargetInRange_OutsideRange_ReturnsFailure()
        {
            var cond = new TargetInRangeCondition();
            var info = TestHelpers.MakeMonsterInfo("MID_S");
            var inst = new MonsterInstance("S", info, new FakeStatContainer());
            inst.SetPosition(Vector3.zero);

            _ctx.Self = inst;
            _ctx.Target = TestHelpers.MakeMonsterInfo("MID_T");
            _ctx.TargetPosition = new Vector3(10f, 0f, 0f);

            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "5", "", ""));
        }

        // --- HasBuffCondition ---

        [Test]
        public void HasBuff_BlackboardKeyTrue_ReturnsSuccess()
        {
            var cond = new HasBuffCondition();
            _ctx.SetBlackboard("buff_BUFF_RAGE", true);
            Assert.AreEqual(BehaviorNodeStatus.Success, cond.Tick(_ctx, "BUFF_RAGE", "", ""));
        }

        [Test]
        public void HasBuff_BlackboardKeyMissing_ReturnsFailure()
        {
            var cond = new HasBuffCondition();
            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "BUFF_RAGE", "", ""));
        }

        [Test]
        public void HasBuff_EmptyBuffId_ReturnsFailure()
        {
            var cond = new HasBuffCondition();
            Assert.AreEqual(BehaviorNodeStatus.Failure, cond.Tick(_ctx, "", "", ""));
        }
    }
}
