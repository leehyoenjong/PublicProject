using NUnit.Framework;

namespace PublicFramework.Tests.Character
{
    public class PartyInstanceTests
    {
        [Test]
        public void Ctor_DefaultName_EqualsDeckId()
        {
            var p = new PartyInstance("deck_1", 3);
            Assert.AreEqual("deck_1", p.DeckId);
            Assert.AreEqual("deck_1", p.Name);
            Assert.AreEqual(3, p.PartySize);
            Assert.IsTrue(p.IsEmpty);
        }

        [Test]
        public void Ctor_CustomName_UsesProvidedName()
        {
            var p = new PartyInstance("deck_1", 2, "보스용");
            Assert.AreEqual("보스용", p.Name);
        }

        [Test]
        public void Ctor_ZeroOrNegativePartySize_ClampsToOne()
        {
            var zero = new PartyInstance("d1", 0);
            var neg = new PartyInstance("d2", -3);
            Assert.AreEqual(1, zero.PartySize);
            Assert.AreEqual(1, neg.PartySize);
        }
    }
}
