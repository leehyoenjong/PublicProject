using NUnit.Framework;

namespace PublicFramework.Tests.Pet
{
    public class PetInstanceTests
    {
        [Test]
        public void New_StartsUnequipped_LevelOne()
        {
            var info = TestHelpers.MakePetInfo("pet_fox", skillSlotMax: 2);
            var inst = new PetInstance("inst_001", info, null);

            Assert.AreEqual("inst_001", inst.InstanceId);
            Assert.AreSame(info, inst.Info);
            Assert.AreEqual(1, inst.Level);
            Assert.IsFalse(inst.IsEquipped);
            Assert.AreEqual(-1, inst.EquippedSlotIndex);
            Assert.IsNotNull(inst.EquippedSkills);
            Assert.AreEqual(2, inst.EquippedSkills.Count);
        }

        [Test]
        public void IUnitInstance_Unit_IsInfo()
        {
            var info = TestHelpers.MakePetInfo("pet_owl");
            var inst = new PetInstance("inst_owl", info, null);

            IUnitInstance ui = inst;
            Assert.AreSame(info, ui.Unit);
        }

        [Test]
        public void EquippedSkills_ZeroSlot_StaysEmpty()
        {
            var info = TestHelpers.MakePetInfo("pet_slime", skillSlotMax: 0);
            var inst = new PetInstance("inst_slime", info, null);

            Assert.AreEqual(0, inst.EquippedSkills.Count);
            Assert.IsFalse(inst.TrySetSkill(0, null));
        }

        [Test]
        public void SetEquippedSlot_FlagsAsEquipped()
        {
            var info = TestHelpers.MakePetInfo("pet_fox");
            var inst = new PetInstance("inst_fox", info, null);

            inst.SetEquippedSlot(2);
            Assert.IsTrue(inst.IsEquipped);
            Assert.AreEqual(2, inst.EquippedSlotIndex);

            inst.ClearEquippedSlot();
            Assert.IsFalse(inst.IsEquipped);
            Assert.AreEqual(-1, inst.EquippedSlotIndex);
        }
    }
}
