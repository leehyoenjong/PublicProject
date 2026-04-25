using System.Collections.Generic;

namespace PublicFramework.Tests
{
    internal class FakeDropContext : IDropContext
    {
        public int PlayerLevel { get; set; }
        public Dictionary<int, int> DropCounts { get; } = new();

        public int GetDropCount(int itemMID) =>
            DropCounts.TryGetValue(itemMID, out int v) ? v : 0;

        public void AddDropCount(int itemMID, int count)
        {
            DropCounts[itemMID] = GetDropCount(itemMID) + count;
        }
    }
}
