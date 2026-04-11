using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 퀘스트 시스템 서비스 인터페이스
    /// </summary>
    public interface IQuestSystem : IService
    {
        bool AcceptQuest(string questId);
        bool AbandonQuest(string questId);
        bool ClaimReward(string questId);
        IReadOnlyList<IQuestInstance> GetQuests(QuestState? stateFilter = null, QuestType? typeFilter = null);
        IQuestInstance GetProgress(string questId);
        void RegisterQuest(QuestData questData);
        void CheckUnlocks();
        void ResetDaily();
        void ResetWeekly();
        void SetRewardHandler(IRewardHandler handler);
        void NotifyConditionProgress(ConditionType type, string targetId, int amount);
    }
}
