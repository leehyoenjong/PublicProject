using NUnit.Framework;

namespace PublicFramework.Tests.Monster
{
    public class DropResolverTests
    {
        private DefaultDropTableResolver _resolver;
        private FakeDropContext _context;

        [SetUp]
        public void SetUp()
        {
            _resolver = new DefaultDropTableResolver();
            _context = new FakeDropContext { PlayerLevel = 10 };
        }

        [Test]
        public void Weight100_AlwaysDrops()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 1001, weight: 100, minCount: 5, maxCount: 5));
            var rng = new FakeRandomProvider(99);  // < 100 → drops

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(1, result.Drops.Count);
            Assert.AreEqual(1001, result.Drops[0].ItemMID);
            Assert.AreEqual(5, result.Drops[0].Count);
        }

        [Test]
        public void RollGreaterOrEqualWeight_DoesNotDrop()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 5001, weight: 5));
            var rng = new FakeRandomProvider(5);  // 5 NOT < 5

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(0, result.Drops.Count);
        }

        [Test]
        public void IndependentRolls_PerEntry()
        {
            // 2 항목: 둘 다 100% — 둘 다 드롭되어야 함
            var table = TestHelpers.MakeDropTableData("drop_dragon",
                TestHelpers.MakeDropEntry(itemMID: 10001, weight: 100, minCount: 5000, maxCount: 5000),
                TestHelpers.MakeDropEntry(itemMID: 40001, weight: 100, minCount: 1, maxCount: 1));
            var rng = new FakeRandomProvider(0, 0);

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(2, result.Drops.Count);
            Assert.AreEqual(10001, result.Drops[0].ItemMID);
            Assert.AreEqual(40001, result.Drops[1].ItemMID);
        }

        [Test]
        public void MinPlayerLevel_NotMet_Skips()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 50001, weight: 100, minPlayerLevel: 20));
            _context.PlayerLevel = 10;
            var rng = new FakeRandomProvider(0);

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(0, result.Drops.Count);
        }

        [Test]
        public void RepeatLimit_Reached_Skips()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 50001, weight: 100, repeatLimit: 1));
            _context.AddDropCount(50001, 1);  // 이미 1회 드롭
            var rng = new FakeRandomProvider(0);

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(0, result.Drops.Count);
        }

        [Test]
        public void CountRange_PicksWithinBounds()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 10001, weight: 100, minCount: 80, maxCount: 120));
            // 첫 번째 NextInt = weight roll (0 → 항상 드롭),
            // 두 번째 NextInt = count roll (range [80, 121) → 100 반환)
            var rng = new FakeRandomProvider(0, 100);

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(1, result.Drops.Count);
            Assert.AreEqual(100, result.Drops[0].Count);
        }

        [Test]
        public void Weight0OrNegative_Skipped()
        {
            var table = TestHelpers.MakeDropTableData("drop_t",
                TestHelpers.MakeDropEntry(itemMID: 1, weight: 0),
                TestHelpers.MakeDropEntry(itemMID: 2, weight: -10),
                TestHelpers.MakeDropEntry(itemMID: 3, weight: 100));
            var rng = new FakeRandomProvider(0);

            var result = _resolver.Resolve(table, _context, rng);

            Assert.AreEqual(1, result.Drops.Count);
            Assert.AreEqual(3, result.Drops[0].ItemMID);
        }

        [Test]
        public void NullTable_ReturnsEmpty()
        {
            var rng = new FakeRandomProvider();
            var result = _resolver.Resolve(null, _context, rng);
            Assert.IsNotNull(result.Drops);
            Assert.AreEqual(0, result.Drops.Count);
        }
    }
}
