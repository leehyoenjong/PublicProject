using NUnit.Framework;

namespace PublicFramework.Tests.Monster
{
    public class MonsterInfoTests
    {
        [Test]
        public void Defaults_AreReadable_WithoutCrash()
        {
            var info = TestHelpers.MakeMonsterInfo("mon_test");

            Assert.AreEqual("mon_test", info.MID);
            Assert.AreEqual("mon_test", info.UnitId);
            Assert.AreEqual(MonsterType.Normal, info.Type);
            Assert.AreEqual(1, info.Level);
            Assert.AreEqual(0, info.ExpReward);
            Assert.AreEqual(0, info.GoldReward);
            Assert.IsNotNull(info.BaseSkills);
            Assert.AreEqual(0, info.BaseSkills.Count);
            Assert.IsNotNull(info.OnSpawnEvents);
            Assert.AreEqual(0, info.OnSpawnEvents.Count);
            Assert.IsNotNull(info.OnDeathEvents);
            Assert.AreEqual(0, info.OnDeathEvents.Count);
        }

        [Test]
        public void IUnit_UnitId_EqualsMID()
        {
            var info = TestHelpers.MakeMonsterInfo("mon_slime", baseStatMID: "stat_mon_slime");

            IUnit unit = info;
            Assert.AreEqual("mon_slime", unit.UnitId);
            Assert.AreEqual("stat_mon_slime", unit.BaseStatMID);
        }

        [Test]
        public void Boss_FullPayload_RoundTrips()
        {
            var info = TestHelpers.MakeMonsterInfo(
                "mon_dragon",
                type: MonsterType.Boss,
                nameKey: 6005,
                descKey: 6006,
                iconAddress: "icons/monsters/dragon",
                classTag: "Beast",
                elementTag: "Fire",
                baseStatMID: "stat_mon_dragon",
                dropTableMID: "drop_dragon",
                aiPresetMID: "ai_boss_pattern",
                level: 30,
                expReward: 5000,
                goldReward: 3000,
                onSpawnEvents: new[] { "Intro:boss", "Cinematic:dragon" },
                onDeathEvents: new[] { "StageClear" },
                hitReactionId: "hit_boss"
            );

            Assert.AreEqual(MonsterType.Boss, info.Type);
            Assert.AreEqual(6005, info.NameKey);
            Assert.AreEqual(6006, info.DescKey);
            Assert.AreEqual("Beast", info.ClassTag);
            Assert.AreEqual("Fire", info.ElementTag);
            Assert.AreEqual("drop_dragon", info.DropTableMID);
            Assert.AreEqual("ai_boss_pattern", info.AIPresetMID);
            Assert.AreEqual(30, info.Level);
            Assert.AreEqual(5000, info.ExpReward);
            Assert.AreEqual(3000, info.GoldReward);
            Assert.AreEqual(2, info.OnSpawnEvents.Count);
            Assert.AreEqual("Intro:boss", info.OnSpawnEvents[0]);
            Assert.AreEqual(1, info.OnDeathEvents.Count);
            Assert.AreEqual("StageClear", info.OnDeathEvents[0]);
            Assert.AreEqual("hit_boss", info.HitReactionId);
        }
    }
}
