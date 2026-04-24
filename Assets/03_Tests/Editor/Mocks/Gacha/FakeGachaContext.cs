using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IGachaContext. PlayerLevel + 클리어한 퀘스트 MID 세트.</summary>
    public class FakeGachaContext : IGachaContext
    {
        private readonly HashSet<int> _clearedQuests = new HashSet<int>();

        public int PlayerLevel { get; set; } = 1;

        public bool IsQuestCleared(int questMID) => _clearedQuests.Contains(questMID);

        public void MarkQuestCleared(int questMID)
        {
            _clearedQuests.Add(questMID);
        }

        public void ClearQuestProgress()
        {
            _clearedQuests.Clear();
        }
    }
}
