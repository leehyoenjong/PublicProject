using System;
using NUnit.Framework;

namespace PublicFramework.Tests.Save
{
    public class SaveSlotTests
    {
        [Test]
        public void LastSavedAt_Empty_ReturnsMinValue()
        {
            var slot = new SaveSlot(0);

            Assert.AreEqual(DateTime.MinValue, slot.LastSavedAt);
        }

        [Test]
        public void LastSavedAt_Roundtrip_PreservesUtcInstant()
        {
            var slot = new SaveSlot(0);
            DateTime original = new DateTime(2026, 4, 24, 9, 0, 0, DateTimeKind.Utc);

            slot.LastSavedAt = original;

            Assert.AreEqual(original, slot.LastSavedAt.ToUniversalTime());
        }

        [Test]
        public void LastSavedAt_Iso8601WithUtcOffset_ParsesToSameUtcInstant()
        {
            var slot = new SaveSlot(0);
            TestHelpers.SetPrivateField(slot, "_lastSavedAt", "2026-04-24T09:00:00+00:00");

            DateTime parsed = slot.LastSavedAt;

            DateTime expected = new DateTime(2026, 4, 24, 9, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(expected, parsed.ToUniversalTime());
        }
    }
}
