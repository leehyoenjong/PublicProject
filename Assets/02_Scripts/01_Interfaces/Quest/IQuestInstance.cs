using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 인스턴스 인터페이스
    /// </summary>
    public interface IQuestInstance
    {
        string QuestId { get; }
        QuestType QuestType { get; }
        QuestState State { get; }
        string DisplayName { get; }
        string Description { get; }
        float Progress { get; }
        bool IsCompleted { get; }
        IReadOnlyList<IConditionProgress> GetConditions();
        IReadOnlyList<QuestReward> GetRewards();
    }
}
