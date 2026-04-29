using NUnit.Framework;

namespace PublicFramework.Tests.Stat
{
    public class StatSystemTests
    {
        private FakeEventBus _bus;
        private StatSystem _system;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _system = new StatSystem(_bus);
        }

        [Test]
        public void CreateContainer_NewOwner_Returns()
        {
            var c = _system.CreateContainer("u1");
            Assert.IsNotNull(c);
            Assert.AreEqual("u1", c.OwnerId);
            Assert.AreEqual(1, _system.Count);
        }

        [Test]
        public void CreateContainer_DuplicateOwner_ReturnsExisting()
        {
            var first = _system.CreateContainer("u1");
            var second = _system.CreateContainer("u1");
            Assert.AreSame(first, second);
            Assert.AreEqual(1, _system.Count);
        }

        [Test]
        public void CreateContainer_WithLevel_SetsLevel()
        {
            var c = _system.CreateContainer("u1", level: 10);
            Assert.AreEqual(10, c.Level);
        }

        [Test]
        public void GetContainer_Unknown_ReturnsNull()
        {
            Assert.IsNull(_system.GetContainer("missing"));
        }

        [Test]
        public void RemoveContainer_Existing_ReturnsTrue()
        {
            _system.CreateContainer("u1");
            Assert.IsTrue(_system.RemoveContainer("u1"));
            Assert.AreEqual(0, _system.Count);
        }

        [Test]
        public void RemoveContainer_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.RemoveContainer("ghost"));
        }

        [Test]
        public void TickAll_RegensAllContainers()
        {
            var c1 = _system.CreateContainer("u1");
            c1.SetBaseValue(StatType.HP, 200f);
            c1.SetBaseValue(StatType.HPRegen, 10f);
            c1.SetCurrentHP(50f);

            var c2 = _system.CreateContainer("u2");
            c2.SetBaseValue(StatType.HP, 100f);
            c2.SetBaseValue(StatType.HPRegen, 5f);
            c2.SetCurrentHP(30f);

            _system.TickAll(2f);

            Assert.AreEqual(70f, c1.CurrentHP, 0.001f);  // 50 + 10*2
            Assert.AreEqual(40f, c2.CurrentHP, 0.001f);  // 30 + 5*2
        }
    }
}
