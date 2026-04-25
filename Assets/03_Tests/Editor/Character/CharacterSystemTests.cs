using System.Collections.Generic;
using NUnit.Framework;

namespace PublicFramework.Tests.Character
{
    public class CharacterSystemTests
    {
        private FakeEventBus _events;
        private FakeStatContainer _stats;
        private CharacterInfo _warrior;
        private CharacterInfo _mage;
        private CharacterSystem _system;

        [SetUp]
        public void SetUp()
        {
            _events = new FakeEventBus();
            _stats = new FakeStatContainer();

            _warrior = TestHelpers.MakeCharacterInfo(
                itemMID: 40001,
                role: CharacterRole.Tank,
                classTag: "Warrior",
                elementTag: "Earth",
                slotStrategy: SkillSlotStrategy.Fixed,
                slotValue: 3,
                dialogues: new[]
                {
                    TestHelpers.MakeCharacterDialogue(DialogueEvent.OnAcquire, 5001),
                    TestHelpers.MakeCharacterDialogue(DialogueEvent.OnWin, 5002),
                    TestHelpers.MakeCharacterDialogue(DialogueEvent.OnIdle, 0),
                },
                profiles: new[]
                {
                    TestHelpers.MakeCharacterProfile("birth", "Eastwood"),
                    TestHelpers.MakeCharacterProfile("age", "21"),
                });

            _mage = TestHelpers.MakeCharacterInfo(
                itemMID: 40002,
                role: CharacterRole.Dealer,
                slotStrategy: SkillSlotStrategy.ByLevel,
                slotValue: 10);

            _system = new CharacterSystem(eventBus: _events);
            _system.Initialize(new List<ICharacterInfo> { _warrior, _mage });
        }

        [Test]
        public void GetInfo_KnownMID_ReturnsInfo()
        {
            Assert.AreSame(_warrior, _system.GetInfo(40001));
            Assert.AreSame(_mage, _system.GetInfo(40002));
        }

        [Test]
        public void GetInfo_UnknownMID_ReturnsNull()
        {
            Assert.IsNull(_system.GetInfo(99999));
        }

        [Test]
        public void Create_Success_StoresInstance_AndPublishes()
        {
            var inst = _system.Create(40001, "inst_1", _stats, level: 5, rarity: Rarity.Rare);
            Assert.IsNotNull(inst);
            Assert.AreEqual("inst_1", inst.InstanceId);
            Assert.AreEqual(5, inst.Level);
            Assert.AreEqual(Rarity.Rare, inst.Rarity);
            Assert.AreSame(_stats, inst.Stats);
            Assert.AreSame(inst, _system.Get("inst_1"));
            Assert.AreEqual(1, _events.GetPublished<CharacterCreatedEvent>().Count);
        }

        [Test]
        public void Create_UnknownMID_ReturnsNull()
        {
            Assert.IsNull(_system.Create(99999, "inst_X", _stats));
        }

        [Test]
        public void Create_DuplicateInstanceId_ReturnsNull()
        {
            _system.Create(40001, "inst_1", _stats);
            Assert.IsNull(_system.Create(40002, "inst_1", _stats));
        }

        [Test]
        public void Create_EmptyInstanceId_ReturnsNull()
        {
            Assert.IsNull(_system.Create(40001, "", _stats));
            Assert.IsNull(_system.Create(40001, null, _stats));
        }

        [Test]
        public void Remove_Existing_ReturnsTrue_AndPublishes()
        {
            _system.Create(40001, "inst_1", _stats);
            _events.Clear();
            Assert.IsTrue(_system.Remove("inst_1"));
            Assert.IsNull(_system.Get("inst_1"));
            Assert.AreEqual(1, _events.GetPublished<CharacterRemovedEvent>().Count);
        }

        [Test]
        public void Remove_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.Remove("inst_X"));
        }

        [Test]
        public void SetLevel_Changes_AndPublishes()
        {
            _system.Create(40001, "inst_1", _stats);
            _events.Clear();
            _system.SetLevel("inst_1", 10);
            Assert.AreEqual(10, _system.Get("inst_1").Level);
            Assert.AreEqual(1, _events.GetPublished<CharacterLevelChangedEvent>().Count);
        }

        [Test]
        public void SetLevel_SameValue_NoEvent()
        {
            _system.Create(40001, "inst_1", _stats, level: 5);
            _events.Clear();
            _system.SetLevel("inst_1", 5);
            Assert.AreEqual(0, _events.GetPublished<CharacterLevelChangedEvent>().Count);
        }

        [Test]
        public void SetAwakening_Changes_AndPublishes()
        {
            _system.Create(40001, "inst_1", _stats);
            _events.Clear();
            _system.SetAwakening("inst_1", 2);
            Assert.AreEqual(2, _system.Get("inst_1").Awakening);
            Assert.AreEqual(1, _events.GetPublished<CharacterAwakeningChangedEvent>().Count);
        }

        [Test]
        public void SetAwakening_SameValue_NoEvent()
        {
            _system.Create(40001, "inst_1", _stats);
            _system.SetAwakening("inst_1", 2);
            _events.Clear();
            _system.SetAwakening("inst_1", 2);
            Assert.AreEqual(0, _events.GetPublished<CharacterAwakeningChangedEvent>().Count);
        }

        [Test]
        public void CalculateSlotCount_UsesDefaultResolver()
        {
            var inst = _system.Create(40001, "inst_1", _stats);
            Assert.AreEqual(3, _system.CalculateSlotCount(inst));

            var mage = _system.Create(40002, "inst_2", _stats, level: 11);
            Assert.AreEqual(2, _system.CalculateSlotCount(mage));
        }

        [Test]
        public void CalculateSlotCount_NullInstance_ReturnsZero()
        {
            Assert.AreEqual(0, _system.CalculateSlotCount(null));
        }

        [Test]
        public void GetDialogueLine_MatchingEvent_ReturnsKey()
        {
            var inst = _system.Create(40001, "inst_1", _stats);
            Assert.AreEqual(5001, _system.GetDialogueLine(inst, DialogueEvent.OnAcquire));
            Assert.AreEqual(5002, _system.GetDialogueLine(inst, DialogueEvent.OnWin));
        }

        [Test]
        public void GetDialogueLine_EmptyLineOrUnknownEvent_ReturnsZero()
        {
            var inst = _system.Create(40001, "inst_1", _stats);
            Assert.AreEqual(0, _system.GetDialogueLine(inst, DialogueEvent.OnIdle));
            Assert.AreEqual(0, _system.GetDialogueLine(inst, DialogueEvent.OnLevelUp));
        }

        [Test]
        public void GetProfileValue_MatchingKey_ReturnsValue()
        {
            var inst = _system.Create(40001, "inst_1", _stats);
            Assert.AreEqual("Eastwood", _system.GetProfileValue(inst, "birth"));
            Assert.AreEqual("21", _system.GetProfileValue(inst, "age"));
        }

        [Test]
        public void GetProfileValue_UnknownKey_ReturnsNull()
        {
            var inst = _system.Create(40001, "inst_1", _stats);
            Assert.IsNull(_system.GetProfileValue(inst, "unknown"));
            Assert.IsNull(_system.GetProfileValue(inst, ""));
            Assert.IsNull(_system.GetProfileValue(inst, null));
        }

        [Test]
        public void GetProfileEntry_Localized_ReturnsEntryWithValueKey()
        {
            var info = TestHelpers.MakeCharacterInfo(40010, profiles: new[]
            {
                TestHelpers.MakeCharacterProfile("birth", "", 6301),
                TestHelpers.MakeCharacterProfile("age", "24", 0),
            });
            _system.Initialize(new List<ICharacterInfo> { info });
            var inst = _system.Create(40010, "inst_10", _stats);

            var birth = _system.GetProfileEntry(inst, "birth");
            Assert.IsNotNull(birth);
            Assert.IsTrue(birth.UsesLocalization);
            Assert.AreEqual(6301, birth.ValueKey);

            var age = _system.GetProfileEntry(inst, "age");
            Assert.IsNotNull(age);
            Assert.IsFalse(age.UsesLocalization);
            Assert.AreEqual("24", age.Value);

            Assert.IsNull(_system.GetProfileEntry(inst, "unknown"));
        }

        [Test]
        public void CustomSlotResolver_OverridesDefault()
        {
            var sys = new CharacterSystem(slotResolver: new ConstantSlotResolver(99), eventBus: _events);
            sys.Initialize(new List<ICharacterInfo> { _warrior });
            var inst = sys.Create(40001, "inst_c", _stats);
            Assert.AreEqual(99, sys.CalculateSlotCount(inst));
        }

        [Test]
        public void Create_InjectsBaseSkills()
        {
            var skill = TestHelpers.MakeSkillData("skill_base_01");
            var info = TestHelpers.MakeCharacterInfo(40003, baseSkills: new[] { skill });
            _system.Initialize(new List<ICharacterInfo> { info });
            var inst = _system.Create(40003, "inst_3", _stats);
            Assert.AreEqual(1, inst.EquippedSkills.Count);
            Assert.AreSame(skill, inst.EquippedSkills[0]);
        }

        private class ConstantSlotResolver : ISkillSlotResolver
        {
            private readonly int _count;
            public ConstantSlotResolver(int count) { _count = count; }
            public int Resolve(ICharacterInfo info, int level, int awakening, Rarity rarity) => _count;
        }
    }
}
