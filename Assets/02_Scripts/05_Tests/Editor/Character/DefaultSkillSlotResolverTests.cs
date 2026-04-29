using NUnit.Framework;

namespace PublicFramework.Tests.Character
{
    public class DefaultSkillSlotResolverTests
    {
        private DefaultSkillSlotResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new DefaultSkillSlotResolver();
        }

        [Test]
        public void Resolve_NullInfo_ReturnsZero()
        {
            Assert.AreEqual(0, _resolver.Resolve(null, 1, 0, Rarity.Common));
        }

        [Test]
        public void Resolve_Fixed_ReturnsSlotValue()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.Fixed, slotValue: 4);
            Assert.AreEqual(4, _resolver.Resolve(info, 1, 0, Rarity.Common));
            Assert.AreEqual(4, _resolver.Resolve(info, 50, 3, Rarity.Mythic));
        }

        [Test]
        public void Resolve_Fixed_NegativeValue_ClampsToZero()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.Fixed, slotValue: -5);
            Assert.AreEqual(0, _resolver.Resolve(info, 1, 0, Rarity.Common));
        }

        [Test]
        public void Resolve_ByLevel_IncrementsAtBoundaries()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.ByLevel, slotValue: 10);
            Assert.AreEqual(1, _resolver.Resolve(info, 1, 0, Rarity.Common));
            Assert.AreEqual(1, _resolver.Resolve(info, 10, 0, Rarity.Common));
            Assert.AreEqual(2, _resolver.Resolve(info, 11, 0, Rarity.Common));
            Assert.AreEqual(3, _resolver.Resolve(info, 21, 0, Rarity.Common));
        }

        [Test]
        public void Resolve_ByLevel_ZeroValue_DefaultsToOne()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.ByLevel, slotValue: 0);
            Assert.AreEqual(1, _resolver.Resolve(info, 100, 0, Rarity.Common));
        }

        [Test]
        public void Resolve_ByRarity_ScalesWithRarityOrder()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.ByRarity, slotValue: 1);
            Assert.AreEqual(1, _resolver.Resolve(info, 1, 0, Rarity.Common));
            Assert.AreEqual(2, _resolver.Resolve(info, 1, 0, Rarity.Uncommon));
            Assert.AreEqual(4, _resolver.Resolve(info, 1, 0, Rarity.Epic));
            Assert.AreEqual(6, _resolver.Resolve(info, 1, 0, Rarity.Mythic));
        }

        [Test]
        public void Resolve_ByAwakening_ScalesWithAwakening()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.ByAwakening, slotValue: 1);
            Assert.AreEqual(1, _resolver.Resolve(info, 1, 0, Rarity.Common));
            Assert.AreEqual(3, _resolver.Resolve(info, 1, 2, Rarity.Common));
            Assert.AreEqual(6, _resolver.Resolve(info, 1, 5, Rarity.Common));
        }

        [Test]
        public void Resolve_ByAwakening_NegativeAwakening_ClampsToZero()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.ByAwakening, slotValue: 2);
            Assert.AreEqual(1, _resolver.Resolve(info, 1, -3, Rarity.Common));
        }

        [Test]
        public void Resolve_Custom_ReturnsZero()
        {
            var info = TestHelpers.MakeCharacterInfo(1, slotStrategy: SkillSlotStrategy.Custom, slotValue: 99);
            Assert.AreEqual(0, _resolver.Resolve(info, 50, 5, Rarity.Legendary));
        }
    }
}
