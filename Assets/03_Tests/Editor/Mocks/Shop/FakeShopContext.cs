using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IShopContext. 레벨/퀘스트 클리어 셋.</summary>
    public class FakeShopContext : IShopContext
    {
        private readonly HashSet<int> _clearedQuests = new HashSet<int>();

        public int PlayerLevel { get; set; } = 1;

        public bool IsQuestCleared(int questMID) => _clearedQuests.Contains(questMID);

        public void MarkQuestCleared(int questMID) => _clearedQuests.Add(questMID);
    }
}
