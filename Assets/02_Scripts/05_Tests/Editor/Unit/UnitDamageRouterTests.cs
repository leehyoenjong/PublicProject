using NUnit.Framework;

namespace PublicFramework.Tests.Unit
{
    public class UnitDamageRouterTests
    {
        private FakeStatContainer _stats;
        private FakeEventBus _events;

        [SetUp]
        public void SetUp()
        {
            _stats = new FakeStatContainer();
            _stats.SetFinalValue(StatType.HP, 100f);
            _stats.CurrentHP = 100f;
            _events = new FakeEventBus();
        }

        [Test]
        public void Damage_Reduces_Hp_AndReturnsAlive()
        {
            bool alive = UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, -30f, "skill:fire");

            Assert.IsTrue(alive);
            Assert.AreEqual(70f, _stats.CurrentHP);
        }

        [Test]
        public void Heal_Adds_ClampedToMaxHp()
        {
            _stats.CurrentHP = 80f;

            bool alive = UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, +50f, "skill:heal");

            Assert.IsTrue(alive);
            Assert.AreEqual(100f, _stats.CurrentHP, "MaxHP(100) 으로 클램프되어야 함");
        }

        [Test]
        public void FatalDamage_TriggersDiedEventOnce_ReturnsDead()
        {
            bool alive = UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, -200f, "skill:nuke");

            Assert.IsFalse(alive);
            Assert.AreEqual(0f, _stats.CurrentHP);
            Assert.AreEqual(1, _events.GetPublished<UnitDiedEvent>().Count);
            Assert.AreEqual("unit_a", _events.GetPublished<UnitDiedEvent>()[0].InstanceId);
            Assert.AreEqual("char_warrior", _events.GetPublished<UnitDiedEvent>()[0].UnitId);
            Assert.AreEqual("skill:nuke", _events.GetPublished<UnitDiedEvent>()[0].LastDamageSource);
        }

        [Test]
        public void AlreadyDead_NoChangeNoEvents()
        {
            _stats.CurrentHP = 0f;

            bool alive = UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", false, -10f, "skill:overkill");

            Assert.IsFalse(alive);
            Assert.AreEqual(0f, _stats.CurrentHP, "HP 변경 없어야 함");
            Assert.AreEqual(0, _events.GetPublished<UnitHpChangedEvent>().Count);
            Assert.AreEqual(0, _events.GetPublished<UnitDiedEvent>().Count);
        }

        [Test]
        public void NullStats_ReturnsWasAliveAsIs_NoEvents()
        {
            bool alive = UnitDamageRouter.Apply(null, _events, "unit_a", "char_warrior", true, -50f, "skill:x");

            Assert.IsTrue(alive, "stats null 이면 wasAlive 그대로 반환");
            Assert.AreEqual(0, _events.AllPublished.Count);
        }

        [Test]
        public void Publishes_HpChanged_WithCorrectPayload()
        {
            UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, -25f, "skill:slash");

            var hpEvents = _events.GetPublished<UnitHpChangedEvent>();
            Assert.AreEqual(1, hpEvents.Count);
            Assert.AreEqual("unit_a", hpEvents[0].InstanceId);
            Assert.AreEqual(100f, hpEvents[0].OldHp);
            Assert.AreEqual(75f, hpEvents[0].NewHp);
            Assert.AreEqual("skill:slash", hpEvents[0].Source);
        }

        [Test]
        public void Damage_DoesNotPublishDied_IfStillAlive()
        {
            UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, -50f, "skill:slash");

            Assert.AreEqual(0, _events.GetPublished<UnitDiedEvent>().Count);
        }

        [Test]
        public void NegativeOverkill_ClampsToZero_NotNegative()
        {
            UnitDamageRouter.Apply(_stats, _events, "unit_a", "char_warrior", true, -500f, "skill:overkill");

            Assert.AreEqual(0f, _stats.CurrentHP, "음수로 떨어지지 않고 0 으로 클램프");
        }
    }
}
