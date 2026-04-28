using NUnit.Framework;

namespace PublicFramework.Tests.Monster.AI
{
    public class DefaultBehaviorActionsTests
    {
        private BehaviorContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _ctx = new BehaviorContext { NowSeconds = 0f };
        }

        [Test]
        public void IdleAction_AlwaysReturnsSuccess()
        {
            var action = new IdleAction();
            Assert.AreEqual(BehaviorNodeStatus.Success, action.Tick(_ctx, "", "", ""));
        }

        [Test]
        public void WaitAction_BeforeElapsed_ReturnsRunning()
        {
            var action = new WaitAction();
            _ctx.NowSeconds = 0f;
            Assert.AreEqual(BehaviorNodeStatus.Running, action.Tick(_ctx, "2", "", ""));

            _ctx.NowSeconds = 1f;
            Assert.AreEqual(BehaviorNodeStatus.Running, action.Tick(_ctx, "2", "", ""));
        }

        [Test]
        public void WaitAction_AfterElapsed_ReturnsSuccess()
        {
            var action = new WaitAction();
            _ctx.NowSeconds = 0f;
            action.Tick(_ctx, "2", "", "");
            _ctx.NowSeconds = 3f;

            Assert.AreEqual(BehaviorNodeStatus.Success, action.Tick(_ctx, "2", "", ""));
        }

        [Test]
        public void WaitAction_ZeroSeconds_ReturnsSuccess()
        {
            var action = new WaitAction();
            Assert.AreEqual(BehaviorNodeStatus.Success, action.Tick(_ctx, "0", "", ""));
        }

        [Test]
        public void MoveToTargetAction_NullTarget_ReturnsFailure()
        {
            var action = new MoveToTargetAction();
            _ctx.Target = null;
            Assert.AreEqual(BehaviorNodeStatus.Failure, action.Tick(_ctx, "", "", ""));
        }

        [Test]
        public void MoveToTargetAction_TargetSet_ReturnsSuccess()
        {
            var action = new MoveToTargetAction();
            _ctx.Target = TestHelpers.MakeMonsterInfo("M_TARGET");
            Assert.AreEqual(BehaviorNodeStatus.Success, action.Tick(_ctx, "", "", ""));
        }

        [Test]
        public void CastSkillAction_NullSkillSystem_ReturnsFailure()
        {
            var action = new CastSkillAction(null);
            _ctx.Self = MakeStubMonster("M1");
            Assert.AreEqual(BehaviorNodeStatus.Failure, action.Tick(_ctx, "SK_FIREBALL", "", ""));
        }

        [Test]
        public void CastSkillAction_NullSkillId_ReturnsFailure()
        {
            var fakeSkill = new FakeSkillSystem();
            var action = new CastSkillAction(fakeSkill);
            _ctx.Self = MakeStubMonster("M1");
            Assert.AreEqual(BehaviorNodeStatus.Failure, action.Tick(_ctx, "", "", ""));
        }

        [Test]
        public void CastSkillAction_NullSelf_ReturnsFailure()
        {
            var fakeSkill = new FakeSkillSystem();
            var action = new CastSkillAction(fakeSkill);
            _ctx.Self = null;
            Assert.AreEqual(BehaviorNodeStatus.Failure, action.Tick(_ctx, "SK_FIREBALL", "", ""));
        }

        [Test]
        public void CastSkillAction_Valid_CallsSkillSystem()
        {
            var fakeSkill = new FakeSkillSystem();
            var action = new CastSkillAction(fakeSkill);
            _ctx.Self = MakeStubMonster("M1");
            _ctx.Target = TestHelpers.MakeMonsterInfo("M_TARGET");

            BehaviorNodeStatus result = action.Tick(_ctx, "SK_FIREBALL", "", "");

            Assert.AreEqual(BehaviorNodeStatus.Success, result);
            Assert.AreEqual(1, fakeSkill.Calls.Count);
            Assert.AreEqual("SK_FIREBALL", fakeSkill.Calls[0].SkillId);
            Assert.AreEqual("M1", fakeSkill.Calls[0].CasterId);
            Assert.AreEqual("M_TARGET", fakeSkill.Calls[0].TargetId);
        }

        [Test]
        public void CastSkillAction_SkillSystemFails_ReturnsFailure()
        {
            var fakeSkill = new FakeSkillSystem { ReturnSuccess = false };
            var action = new CastSkillAction(fakeSkill);
            _ctx.Self = MakeStubMonster("M1");

            Assert.AreEqual(BehaviorNodeStatus.Failure, action.Tick(_ctx, "SK_FIREBALL", "", ""));
        }

        private static MonsterInstance MakeStubMonster(string instanceId)
        {
            var info = TestHelpers.MakeMonsterInfo("MID_" + instanceId);
            return new MonsterInstance(instanceId, info, new FakeStatContainer());
        }
    }
}
