using NUnit.Framework;

namespace PublicFramework.Tests.Combat
{
    public class FactionRulesTests
    {
        [Test]
        public void FriendlyVsEnemy_IsHostile()
        {
            Assert.IsTrue(FactionRules.IsHostile(Faction.Friendly, Faction.Enemy));
            Assert.IsTrue(FactionRules.IsHostile(Faction.Enemy, Faction.Friendly));
        }

        [Test]
        public void SameFaction_NotHostile()
        {
            Assert.IsFalse(FactionRules.IsHostile(Faction.Friendly, Faction.Friendly));
            Assert.IsFalse(FactionRules.IsHostile(Faction.Enemy, Faction.Enemy));
        }

        [Test]
        public void Neutral_NeverHostile()
        {
            Assert.IsFalse(FactionRules.IsHostile(Faction.Neutral, Faction.Friendly));
            Assert.IsFalse(FactionRules.IsHostile(Faction.Friendly, Faction.Neutral));
            Assert.IsFalse(FactionRules.IsHostile(Faction.Neutral, Faction.Enemy));
            Assert.IsFalse(FactionRules.IsHostile(Faction.Neutral, Faction.Neutral));
        }

        [Test]
        public void IsHostile_IsSymmetric()
        {
            foreach (Faction a in System.Enum.GetValues(typeof(Faction)))
                foreach (Faction b in System.Enum.GetValues(typeof(Faction)))
                    Assert.AreEqual(FactionRules.IsHostile(a, b), FactionRules.IsHostile(b, a),
                        $"적대 판정은 대칭이어야 함: {a} vs {b}");
        }
    }
}
