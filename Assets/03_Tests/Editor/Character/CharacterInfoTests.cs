using NUnit.Framework;

namespace PublicFramework.Tests.Character
{
    public class CharacterInfoTests
    {
        [Test]
        public void MakeCharacterInfo_PopulatesAllFields()
        {
            var info = TestHelpers.MakeCharacterInfo(
                itemMID: 40001,
                role: CharacterRole.Healer,
                classTag: "Cleric",
                elementTag: "Light",
                baseStatMID: "stat_healer_01",
                slotStrategy: SkillSlotStrategy.ByRarity,
                slotValue: 1,
                defaultSkinMID: 50001,
                voiceSetId: "vs_01",
                defaultPositionId: "back_center");

            Assert.AreEqual(40001, info.ItemMID);
            Assert.AreEqual("40001", info.UnitId);
            Assert.AreEqual(ItemCategory.Character, info.OwnerCategory);
            Assert.AreEqual(CharacterRole.Healer, info.Role);
            Assert.AreEqual("Cleric", info.ClassTag);
            Assert.AreEqual("Light", info.ElementTag);
            Assert.AreEqual("stat_healer_01", info.BaseStatMID);
            Assert.AreEqual(SkillSlotStrategy.ByRarity, info.SlotStrategy);
            Assert.AreEqual(1, info.SlotValue);
            Assert.AreEqual(50001, info.DefaultSkinMID);
            Assert.AreEqual("vs_01", info.VoiceSetId);
            Assert.AreEqual("back_center", info.DefaultPositionId);
        }

        [Test]
        public void DialogueEntry_HasLine_TrueWhenLineKeyPositive()
        {
            var empty = TestHelpers.MakeCharacterDialogue(DialogueEvent.OnAcquire, 0);
            var filled = TestHelpers.MakeCharacterDialogue(DialogueEvent.OnAcquire, 5);
            Assert.IsFalse(empty.HasLine);
            Assert.IsTrue(filled.HasLine);
        }

        [Test]
        public void ProfileEntry_StoresKeyAndValue()
        {
            var p = TestHelpers.MakeCharacterProfile("birth", "Eastwood");
            Assert.AreEqual("birth", p.Key);
            Assert.AreEqual("Eastwood", p.Value);
        }

        [Test]
        public void ProfileEntry_ValueKeyZero_DoesNotUseLocalization()
        {
            var p = TestHelpers.MakeCharacterProfile("age", "24");
            Assert.AreEqual(0, p.ValueKey);
            Assert.IsFalse(p.UsesLocalization);
        }

        [Test]
        public void ProfileEntry_PositiveValueKey_UsesLocalization()
        {
            var p = TestHelpers.MakeCharacterProfile("birth", "", 6301);
            Assert.AreEqual(6301, p.ValueKey);
            Assert.IsTrue(p.UsesLocalization);
            Assert.AreEqual("", p.Value);
        }
    }
}
