using System;
using NUnit.Framework;

namespace PublicFramework.Tests.Monster
{
    public class BestiaryTests
    {
        [Test]
        public void Register_FirstTime_Succeeds()
        {
            var bestiary = new Bestiary();
            DateTime now = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

            bool ok = bestiary.Register("mon_slime", now);

            Assert.IsTrue(ok);
            Assert.IsTrue(bestiary.IsEntered("mon_slime"));
            Assert.AreEqual(1, bestiary.Entries.Count);
        }

        [Test]
        public void Register_Duplicate_ReturnsFalse()
        {
            var bestiary = new Bestiary();
            DateTime now = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

            bool first = bestiary.Register("mon_slime", now);
            bool second = bestiary.Register("mon_slime", now.AddHours(1));

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(1, bestiary.Entries.Count);
        }

        [Test]
        public void IsEntered_NotRegistered_False()
        {
            var bestiary = new Bestiary();
            Assert.IsFalse(bestiary.IsEntered("mon_unknown"));
        }

        [Test]
        public void Register_NullOrEmpty_RejectsCleanly()
        {
            var bestiary = new Bestiary();
            DateTime now = DateTime.UtcNow;

            Assert.IsFalse(bestiary.Register(null, now));
            Assert.IsFalse(bestiary.Register("", now));
            Assert.AreEqual(0, bestiary.Entries.Count);
        }

        [Test]
        public void Entries_PreservesFirstSeen()
        {
            var bestiary = new Bestiary();
            DateTime now = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);

            bestiary.Register("mon_dragon", now);

            foreach (BestiaryEntry e in bestiary.Entries)
            {
                Assert.AreEqual("mon_dragon", e.MonsterMID);
                Assert.AreEqual(now, e.FirstSeenUtc);
                return;
            }
            Assert.Fail("entries empty");
        }
    }
}
