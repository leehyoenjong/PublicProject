using NUnit.Framework;

namespace PublicFramework.Tests.Pet
{
    public class PetInfoTests
    {
        [Test]
        public void Defaults_AreReadable_WithoutCrash()
        {
            var info = TestHelpers.MakePetInfo("pet_test");

            Assert.AreEqual("pet_test", info.MID);
            Assert.AreEqual("pet_test", info.UnitId);
            Assert.AreEqual(0, info.ItemMID);
            Assert.AreEqual(PetRole.None, info.Roles);
            Assert.AreEqual(0, info.SkillSlotMax);
            Assert.AreEqual(PetFollowStrategy.Behind, info.FollowStrategy);
            Assert.AreEqual(PetCollisionPolicy.Ghost, info.CollisionPolicy);
            Assert.IsNotNull(info.BaseSkills);
            Assert.AreEqual(0, info.BaseSkills.Count);
            Assert.IsNotNull(info.OnAcquireEvents);
            Assert.AreEqual(0, info.OnAcquireEvents.Count);
        }

        [Test]
        public void OwnerCategory_IsPet()
        {
            var info = TestHelpers.MakePetInfo("pet_fox");
            IItemSubtypeInfo subtype = info;
            Assert.AreEqual(ItemCategory.Pet, subtype.OwnerCategory);
        }

        [Test]
        public void IUnit_UnitId_EqualsMID()
        {
            var info = TestHelpers.MakePetInfo("pet_fox", baseStatMID: "stat_pet_fox");

            IUnit unit = info;
            Assert.AreEqual("pet_fox", unit.UnitId);
            Assert.AreEqual("stat_pet_fox", unit.BaseStatMID);
        }

        [Test]
        public void Roles_FlagsCombine_BattleAndFollower()
        {
            var info = TestHelpers.MakePetInfo("pet_fox", roles: PetRole.Battle | PetRole.Follower);

            Assert.IsTrue((info.Roles & PetRole.Battle) != 0);
            Assert.IsTrue((info.Roles & PetRole.Follower) != 0);
            Assert.IsFalse((info.Roles & PetRole.StatBoost) != 0);
        }

        [Test]
        public void FullPayload_RoundTrips()
        {
            var info = TestHelpers.MakePetInfo(
                "pet_dragon",
                itemMID: 70003,
                nameKey: 7005,
                descKey: 7006,
                iconAddress: "icons/pets/dragon",
                roles: PetRole.Battle | PetRole.Follower,
                classTag: "Dragon",
                elementTag: "Fire",
                baseStatMID: "stat_pet_dragon",
                skillSlotMax: 3,
                aiPresetMID: "ai_pet_aerial",
                followStrategy: PetFollowStrategy.Aerial,
                followDistance: 4f,
                catchUpDistance: 12f,
                collisionPolicy: PetCollisionPolicy.Solid,
                onAcquireEvents: new[] { "ev_pet_dragon_acquired" },
                onEquipEvents: new[] { "ev_pet_dragon_equipped" }
            );

            Assert.AreEqual(70003, info.ItemMID);
            Assert.AreEqual(7005, info.NameKey);
            Assert.AreEqual(7006, info.DescKey);
            Assert.AreEqual("icons/pets/dragon", info.IconAddress);
            Assert.AreEqual("Dragon", info.ClassTag);
            Assert.AreEqual("Fire", info.ElementTag);
            Assert.AreEqual(3, info.SkillSlotMax);
            Assert.AreEqual("ai_pet_aerial", info.AIPresetMID);
            Assert.AreEqual(PetFollowStrategy.Aerial, info.FollowStrategy);
            Assert.AreEqual(4f, info.FollowDistance);
            Assert.AreEqual(12f, info.CatchUpDistance);
            Assert.AreEqual(PetCollisionPolicy.Solid, info.CollisionPolicy);
            Assert.AreEqual(1, info.OnAcquireEvents.Count);
            Assert.AreEqual("ev_pet_dragon_acquired", info.OnAcquireEvents[0]);
            Assert.AreEqual(1, info.OnEquipEvents.Count);
        }
    }
}
