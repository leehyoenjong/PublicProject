using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Skill
{
    public class SkillSystemTests
    {
        private const string CASTER = "caster1";
        private const string TARGET = "target1";

        private FakeEventBus _bus;
        private SkillActionRegistry _registry;
        private FakeSkillAction _fakeAction;
        private SkillSystem _system;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _registry = new SkillActionRegistry();
            _fakeAction = new FakeSkillAction(SkillActionType.DealDamage);
            _registry.Register(_fakeAction);
            _system = new SkillSystem(_bus, registry: _registry);
        }

        private static SkillActionEntry MakeAction(SkillActionType type, float delay = 0f)
        {
            return new SkillActionEntry(type, delay, 0f, "", "", "");
        }

        private SkillData RegisterSkill(string skillId, float cooldown = 5f,
            SkillActionEntry[] actions = null, SkillLevelEntry[] levelTable = null)
        {
            SkillData data = TestHelpers.MakeSkillData(skillId, cooldown,
                SkillCostType.None, 0f, actions, levelTable);
            _system.RegisterSkill(data);
            return data;
        }

        // ---------- Cast: 기본 ----------

        [Test]
        public void Cast_UnknownSkill_FailsWithNotFound()
        {
            bool ok = _system.Cast("nope", CASTER, TARGET);

            Assert.IsFalse(ok);
            var fails = _bus.GetPublished<SkillCastFailedEvent>();
            Assert.AreEqual(1, fails.Count);
            Assert.AreEqual("NotFound", fails[0].Reason);
        }

        [Test]
        public void Cast_RegisteredSkill_PublishesCastStarted()
        {
            RegisterSkill("s1");

            _system.Cast("s1", CASTER, TARGET, 1);

            var events = _bus.GetPublished<SkillCastStartedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("s1", events[0].SkillId);
            Assert.AreEqual(CASTER, events[0].CasterId);
            Assert.AreEqual(TARGET, events[0].TargetId);
            Assert.AreEqual(1, events[0].Level);
        }

        [Test]
        public void Cast_RegisteredSkill_StartsCooldown()
        {
            RegisterSkill("s1", cooldown: 8f);
            _system.Cast("s1", CASTER, TARGET);

            ISkillInstance inst = _system.GetInstance(CASTER, "s1");
            Assert.AreEqual(8f, inst.CooldownRemaining, 0.001f);
            Assert.IsFalse(inst.IsReady);
        }

        [Test]
        public void Cast_PublishesCooldownStartedEvent()
        {
            RegisterSkill("s1", cooldown: 6f);
            _system.Cast("s1", CASTER, TARGET);

            var events = _bus.GetPublished<SkillCooldownStartedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(6f, events[0].Duration, 0.001f);
        }

        // ---------- 쿨다운 ----------

        [Test]
        public void Cast_DuringCooldown_FailsWithCooldown()
        {
            RegisterSkill("s1");
            _system.Cast("s1", CASTER, TARGET);

            bool second = _system.Cast("s1", CASTER, TARGET);

            Assert.IsFalse(second);
            var fails = _bus.GetPublished<SkillCastFailedEvent>();
            Assert.AreEqual(1, fails.Count);
            Assert.AreEqual("Cooldown", fails[0].Reason);
        }

        [Test]
        public void Tick_ReducesCooldown()
        {
            RegisterSkill("s1", cooldown: 5f);
            _system.Cast("s1", CASTER, TARGET);

            _system.Tick(2f);

            Assert.AreEqual(3f, _system.GetInstance(CASTER, "s1").CooldownRemaining, 0.001f);
        }

        [Test]
        public void Tick_CooldownReachesZero_PublishesCooldownEnded()
        {
            RegisterSkill("s1", cooldown: 2f);
            _system.Cast("s1", CASTER, TARGET);
            _bus.Clear();

            _system.Tick(3f);

            Assert.IsTrue(_system.GetInstance(CASTER, "s1").IsReady);
            Assert.AreEqual(1, _bus.GetPublished<SkillCooldownEndedEvent>().Count);
        }

        // ---------- 시퀀스 재생 ----------

        [Test]
        public void Cast_ImmediateAction_ExecutesAtCast()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, delay: 0f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            Assert.AreEqual(1, _fakeAction.ExecuteCalls);
        }

        [Test]
        public void Cast_NoActions_PublishesCompletedImmediately()
        {
            RegisterSkill("s1");

            _system.Cast("s1", CASTER, TARGET);

            Assert.AreEqual(1, _bus.GetPublished<SkillCastCompletedEvent>().Count);
        }

        [Test]
        public void Cast_AllImmediateActions_PublishesCompletedImmediately()
        {
            var actions = new[]
            {
                MakeAction(SkillActionType.DealDamage, 0f),
                MakeAction(SkillActionType.DealDamage, 0f)
            };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            Assert.AreEqual(2, _fakeAction.ExecuteCalls);
            Assert.AreEqual(1, _bus.GetPublished<SkillCastCompletedEvent>().Count);
        }

        [Test]
        public void Cast_DelayedAction_NotRunUntilTick()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, delay: 0.5f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);
            Assert.AreEqual(0, _fakeAction.ExecuteCalls);

            _system.Tick(0.6f);
            Assert.AreEqual(1, _fakeAction.ExecuteCalls);
        }

        [Test]
        public void Cast_DelayedAction_PublishesCompletedAfterDelay()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, delay: 0.5f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);
            Assert.AreEqual(0, _bus.GetPublished<SkillCastCompletedEvent>().Count);

            _system.Tick(1f);
            Assert.AreEqual(1, _bus.GetPublished<SkillCastCompletedEvent>().Count);
        }

        // ---------- 레벨 오버라이드 ----------

        [Test]
        public void Cast_LevelOverride_ReplacesCooldown()
        {
            var levels = new[] { new SkillLevelEntry(2, cooldownOverride: 10f, costOverride: 0f, powerMultiplier: 1f) };
            RegisterSkill("s1", cooldown: 5f, levelTable: levels);

            _system.Cast("s1", CASTER, TARGET, 2);

            Assert.AreEqual(10f, _system.GetInstance(CASTER, "s1").CooldownRemaining, 0.001f);
        }

        [Test]
        public void Cast_LevelOverride_AppliesPowerMultiplierToContext()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, 0f) };
            var levels = new[] { new SkillLevelEntry(2, cooldownOverride: 0f, costOverride: 0f, powerMultiplier: 2.5f) };
            RegisterSkill("s1", actions: actions, levelTable: levels);

            _system.Cast("s1", CASTER, TARGET, 2);

            Assert.AreEqual(2.5f, _fakeAction.LastContext.PowerMultiplier, 0.001f);
        }

        [Test]
        public void Cast_NoLevelEntry_UsesBaseCooldown()
        {
            RegisterSkill("s1", cooldown: 7f);

            _system.Cast("s1", CASTER, TARGET, 1);

            Assert.AreEqual(7f, _system.GetInstance(CASTER, "s1").CooldownRemaining, 0.001f);
        }

        [Test]
        public void Cast_NoLevelEntry_PowerMultiplierIsOne()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, 0f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            Assert.AreEqual(1f, _fakeAction.LastContext.PowerMultiplier, 0.001f);
        }

        // ---------- 액션 실행 이벤트 ----------

        [Test]
        public void Cast_PublishesActionExecutedSuccess()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, 0f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            var events = _bus.GetPublished<SkillActionExecutedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0].Success);
            Assert.AreEqual(SkillActionType.DealDamage, events[0].ActionType);
        }

        [Test]
        public void Cast_NoHandler_PublishesActionExecutedFailure()
        {
            LogAssert.Expect(LogType.Error, "[SkillSystem] No handler for Heal");
            var actions = new[] { MakeAction(SkillActionType.Heal, 0f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            var events = _bus.GetPublished<SkillActionExecutedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.IsFalse(events[0].Success);
            Assert.AreEqual("NoHandler", events[0].Error);
        }

        [Test]
        public void Cast_ActionThrows_PublishesActionExecutedFailureWithError()
        {
            LogAssert.Expect(LogType.Error,
                new Regex(@"\[SkillSystem\] Action DealDamage failed: .+"));
            _fakeAction.ThrowOnExecute = true;
            var actions = new[] { MakeAction(SkillActionType.DealDamage, 0f) };
            RegisterSkill("s1", actions: actions);

            _system.Cast("s1", CASTER, TARGET);

            var events = _bus.GetPublished<SkillActionExecutedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.IsFalse(events[0].Success);
            StringAssert.Contains("FakeSkillAction.ThrowOnExecute", events[0].Error);
        }

        // ---------- Execute (쿨/코스트 무시) ----------

        [Test]
        public void Execute_RunsActionsWithoutCooldown()
        {
            var actions = new[] { MakeAction(SkillActionType.DealDamage, 0f) };
            RegisterSkill("s1", cooldown: 100f, actions: actions);

            _system.Execute("s1", CASTER, TARGET, Vector3.zero, Vector3.zero, 1, 3f);
            _system.Execute("s1", CASTER, TARGET, Vector3.zero, Vector3.zero, 1, 3f);

            Assert.AreEqual(2, _fakeAction.ExecuteCalls);
            Assert.AreEqual(3f, _fakeAction.LastContext.PowerMultiplier, 0.001f);
        }

        // ---------- 조회 ----------

        [Test]
        public void GetInstance_AfterCast_Returns()
        {
            RegisterSkill("s1");
            _system.Cast("s1", CASTER, TARGET);

            Assert.IsNotNull(_system.GetInstance(CASTER, "s1"));
        }

        [Test]
        public void GetInstance_UnknownCaster_ReturnsNull()
        {
            Assert.IsNull(_system.GetInstance("nope", "s1"));
        }

        [Test]
        public void GetInstances_ReturnsAllOfCaster()
        {
            RegisterSkill("s1");
            RegisterSkill("s2");
            _system.Cast("s1", CASTER, TARGET);
            _system.Cast("s2", CASTER, TARGET);

            Assert.AreEqual(2, _system.GetInstances(CASTER).Count);
        }
    }
}
