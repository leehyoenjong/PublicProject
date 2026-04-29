using System.Collections.Generic;
using NUnit.Framework;

namespace PublicFramework.Tests.Pet
{
    public class PetSystemTests
    {
        private FakeEventBus _eventBus;
        private PetSystem _system;

        private PetInfo _fox;
        private PetInfo _owl;
        private PetInfo _dragon;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();

            _fox = TestHelpers.MakePetInfo("pet_fox",
                itemMID: 70001,
                roles: PetRole.Battle | PetRole.Follower,
                skillSlotMax: 2,
                onAcquireEvents: new[] { "ev_fox_acquired" },
                onEquipEvents: new[] { "ev_fox_equipped" },
                onUnequipEvents: new[] { "ev_fox_unequipped" });

            _owl = TestHelpers.MakePetInfo("pet_owl",
                itemMID: 70002,
                roles: PetRole.SpecialAbility,
                skillSlotMax: 1);

            _dragon = TestHelpers.MakePetInfo("pet_dragon",
                itemMID: 70003,
                roles: PetRole.Battle | PetRole.Follower,
                skillSlotMax: 3);

            _system = new PetSystem(maxSlots: 3, eventBus: _eventBus);
            _system.Initialize(new List<IPetInfo> { _fox, _owl, _dragon });
        }

        [Test]
        public void GetInfo_KnownMID_ReturnsInstance()
        {
            Assert.AreSame(_fox, _system.GetInfo("pet_fox"));
        }

        [Test]
        public void GetInfo_Unknown_ReturnsNull()
        {
            Assert.IsNull(_system.GetInfo("pet_unknown"));
        }

        [Test]
        public void GetInfoByItemMID_KnownItem_ReturnsInstance()
        {
            Assert.AreSame(_fox, _system.GetInfoByItemMID(70001));
        }

        [Test]
        public void GetInfoByItemMID_Unknown_ReturnsNull()
        {
            Assert.IsNull(_system.GetInfoByItemMID(99999));
        }

        [Test]
        public void Acquire_Known_CreatesInstance_PublishesEvents()
        {
            var inst = _system.Acquire("pet_fox", "fox_001", null);

            Assert.IsNotNull(inst);
            Assert.AreEqual("fox_001", inst.InstanceId);
            Assert.AreSame(_fox, inst.Info);
            Assert.AreEqual(2, inst.EquippedSkills.Count);
            Assert.IsFalse(inst.IsEquipped);

            var acquired = _eventBus.GetPublished<PetAcquiredEvent>();
            Assert.AreEqual(1, acquired.Count);
            Assert.AreEqual(1, acquired[0].TriggeredHookIds.Count);
            Assert.AreEqual("ev_fox_acquired", acquired[0].TriggeredHookIds[0]);
        }

        [Test]
        public void Acquire_DuplicateInstanceId_ReturnsNull()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            var second = _system.Acquire("pet_owl", "fox_001", null);
            Assert.IsNull(second);
        }

        [Test]
        public void Acquire_UnknownPetMID_ReturnsNull()
        {
            Assert.IsNull(_system.Acquire("pet_ghost", "ghost_001", null));
        }

        [Test]
        public void Equip_KnownInstance_FillsSlot_PublishesEvents()
        {
            _system.Acquire("pet_fox", "fox_001", null);

            bool ok = _system.Equip("fox_001", 1);
            Assert.IsTrue(ok);

            IPetInstance equipped = _system.GetEquipped(1);
            Assert.IsNotNull(equipped);
            Assert.AreEqual("fox_001", equipped.InstanceId);
            Assert.IsTrue(equipped.IsEquipped);
            Assert.AreEqual(1, equipped.EquippedSlotIndex);

            var equippedEvents = _eventBus.GetPublished<PetEquippedEvent>();
            Assert.AreEqual(1, equippedEvents.Count);
            Assert.AreEqual(1, equippedEvents[0].SlotIndex);
            Assert.AreEqual("ev_fox_equipped", equippedEvents[0].TriggeredHookIds[0]);
        }

        [Test]
        public void Equip_OutOfRange_ReturnsFalse()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            Assert.IsFalse(_system.Equip("fox_001", 99));
            Assert.IsFalse(_system.Equip("fox_001", -1));
        }

        [Test]
        public void Equip_SwapsExistingOccupant_TriggersUnequipFirst()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Acquire("pet_owl", "owl_001", null);
            _system.Equip("fox_001", 0);

            bool ok = _system.Equip("owl_001", 0);
            Assert.IsTrue(ok);

            Assert.AreEqual("owl_001", _system.GetEquipped(0).InstanceId);
            Assert.IsFalse(_system.Get("fox_001").IsEquipped);

            var unequipped = _eventBus.GetPublished<PetUnequippedEvent>();
            Assert.AreEqual(1, unequipped.Count);
            Assert.AreEqual("pet_fox", unequipped[0].PetMID);
        }

        [Test]
        public void Equip_MoveSamePet_RemovesOldSlot()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Equip("fox_001", 0);

            _system.Equip("fox_001", 2);

            Assert.IsNull(_system.GetEquipped(0));
            Assert.AreEqual("fox_001", _system.GetEquipped(2).InstanceId);
        }

        [Test]
        public void Equip_MultipleSlots_AllOccupied()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Acquire("pet_owl", "owl_001", null);
            _system.Acquire("pet_dragon", "dragon_001", null);

            _system.Equip("fox_001", 0);
            _system.Equip("owl_001", 1);
            _system.Equip("dragon_001", 2);

            Assert.AreEqual("fox_001", _system.GetEquipped(0).InstanceId);
            Assert.AreEqual("owl_001", _system.GetEquipped(1).InstanceId);
            Assert.AreEqual("dragon_001", _system.GetEquipped(2).InstanceId);
        }

        [Test]
        public void Unequip_OccupiedSlot_ClearsAndPublishes()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Equip("fox_001", 0);

            bool ok = _system.Unequip(0);
            Assert.IsTrue(ok);
            Assert.IsNull(_system.GetEquipped(0));
            Assert.IsFalse(_system.Get("fox_001").IsEquipped);

            var unequipped = _eventBus.GetPublished<PetUnequippedEvent>();
            Assert.AreEqual(1, unequipped.Count);
            Assert.AreEqual("ev_fox_unequipped", unequipped[0].TriggeredHookIds[0]);
        }

        [Test]
        public void Unequip_EmptySlot_ReturnsFalse()
        {
            Assert.IsFalse(_system.Unequip(0));
        }

        [Test]
        public void UnequipInstance_NotEquipped_ReturnsFalse()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            Assert.IsFalse(_system.UnequipInstance("fox_001"));
        }

        [Test]
        public void UnequipInstance_Equipped_Clears()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Equip("fox_001", 1);

            Assert.IsTrue(_system.UnequipInstance("fox_001"));
            Assert.IsNull(_system.GetEquipped(1));
        }

        [Test]
        public void Release_EquippedPet_AlsoUnequips()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Equip("fox_001", 0);

            bool ok = _system.Release("fox_001");
            Assert.IsTrue(ok);
            Assert.IsNull(_system.Get("fox_001"));
            Assert.IsNull(_system.GetEquipped(0));

            Assert.AreEqual(1, _eventBus.GetPublished<PetUnequippedEvent>().Count);
            Assert.AreEqual(1, _eventBus.GetPublished<PetReleasedEvent>().Count);
        }

        [Test]
        public void Release_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.Release("ghost"));
        }

        [Test]
        public void SetEquippedSkill_ValidSlot_PublishesChange()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            bool ok = _system.SetEquippedSkill("fox_001", 0, null);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, _eventBus.GetPublished<PetSkillChangedEvent>().Count);
        }

        [Test]
        public void SetEquippedSkill_OutOfRange_ReturnsFalse()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            Assert.IsFalse(_system.SetEquippedSkill("fox_001", 99, null));
        }

        [Test]
        public void SetMaxSlots_Shrink_UnequipsOverflow()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Acquire("pet_owl", "owl_001", null);
            _system.Acquire("pet_dragon", "dragon_001", null);

            _system.Equip("fox_001", 0);
            _system.Equip("owl_001", 1);
            _system.Equip("dragon_001", 2);

            _system.SetMaxSlots(2);

            Assert.AreEqual(2, _system.MaxSlots);
            Assert.IsFalse(_system.Get("dragon_001").IsEquipped);
            Assert.IsTrue(_system.Get("fox_001").IsEquipped);
            Assert.IsTrue(_system.Get("owl_001").IsEquipped);
        }

        [Test]
        public void SetMaxSlots_Grow_KeepsExisting()
        {
            _system.Acquire("pet_fox", "fox_001", null);
            _system.Equip("fox_001", 0);

            _system.SetMaxSlots(5);

            Assert.AreEqual(5, _system.MaxSlots);
            Assert.AreEqual("fox_001", _system.GetEquipped(0).InstanceId);
        }
    }
}
