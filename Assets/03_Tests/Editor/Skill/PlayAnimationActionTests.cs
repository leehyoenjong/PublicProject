using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Skill
{
    public class PlayAnimationActionTests
    {
        private const string CASTER = "caster1";
        private const string TARGET = "target1";

        private FakeEventBus _bus;
        private PlayAnimationAction _action;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _action = new PlayAnimationAction();
        }

        private SkillContext MakeContext(SkillData skillData = null)
        {
            return new SkillContext
            {
                SkillData = skillData,
                CasterId = CASTER,
                TargetId = TARGET,
                EventBus = _bus,
                PowerMultiplier = 1f
            };
        }

        private static SkillActionEntry MakeEntry(string p1 = "", string p2 = "", string p3 = "", float duration = 0f)
        {
            return new SkillActionEntry(SkillActionType.PlayAnimation, 0f, duration, p1, p2, p3);
        }

        [Test]
        public void Execute_ValidAnimKey_PublishesAnimationEvent()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("Cast", events[0].AnimKey);
            Assert.AreEqual(CASTER, events[0].CasterId);
            Assert.AreEqual(TARGET, events[0].TargetId);
        }

        [Test]
        public void Execute_EmptyAnimKey_LogsErrorAndNoEvent()
        {
            LogAssert.Expect(LogType.Error, "[PlayAnimationAction] param1(animKey) is empty");

            _action.Execute(MakeContext(), MakeEntry(p1: ""));

            Assert.AreEqual(0, _bus.GetPublished<SkillAnimationEvent>().Count);
        }

        [Test]
        public void Execute_NullContext_NoCrash()
        {
            Assert.DoesNotThrow(() => _action.Execute(null, MakeEntry(p1: "Cast")));
            Assert.AreEqual(0, _bus.GetPublished<SkillAnimationEvent>().Count);
        }

        [Test]
        public void Execute_NullEntry_NoCrash()
        {
            Assert.DoesNotThrow(() => _action.Execute(MakeContext(), null));
            Assert.AreEqual(0, _bus.GetPublished<SkillAnimationEvent>().Count);
        }

        [Test]
        public void Execute_NoEventBus_LogsWarningAndSkips()
        {
            LogAssert.Expect(LogType.Warning, "[PlayAnimationAction] IEventBus not provided");

            var ctx = MakeContext();
            ctx.EventBus = null;
            _action.Execute(ctx, MakeEntry(p1: "Cast"));

            Assert.AreEqual(0, _bus.GetPublished<SkillAnimationEvent>().Count);
        }

        [Test]
        public void Execute_EmptyParam2_DefaultsToSelf()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast", p2: ""));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual("Self", events[0].TargetRole);
        }

        [Test]
        public void Execute_Param2Target_PassedToEvent()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Hit", p2: "Target"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual("Target", events[0].TargetRole);
        }

        [Test]
        public void Execute_EmptyParam3_LayerIsZero()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast", p3: ""));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual(0, events[0].Layer);
        }

        [Test]
        public void Execute_Param3Numeric_ParsedAsLayer()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast", p3: "2"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual(2, events[0].Layer);
        }

        [Test]
        public void Execute_Param3Invalid_LayerDefaultsToZero()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast", p3: "abc"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual(0, events[0].Layer);
        }

        [Test]
        public void Execute_EntryDuration_PassedToEvent()
        {
            _action.Execute(MakeContext(), MakeEntry(p1: "Cast", duration: 1.5f));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual(1.5f, events[0].Duration, 0.001f);
        }

        [Test]
        public void Execute_WithSkillData_PassesSkillId()
        {
            SkillData data = TestHelpers.MakeSkillData("skill_fireball");
            _action.Execute(MakeContext(data), MakeEntry(p1: "Cast"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.AreEqual("skill_fireball", events[0].SkillId);
        }

        [Test]
        public void Execute_NullSkillData_SkillIdIsNull()
        {
            _action.Execute(MakeContext(skillData: null), MakeEntry(p1: "Cast"));

            var events = _bus.GetPublished<SkillAnimationEvent>();
            Assert.IsNull(events[0].SkillId);
        }
    }
}
