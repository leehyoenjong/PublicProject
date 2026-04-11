using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 업적 시스템 서비스 인터페이스
    /// </summary>
    public interface IAchievementSystem : IService
    {
        bool ClaimReward(string achievementId);
        AchievementState GetState(string achievementId);
        float GetProgress(string achievementId);
        IReadOnlyList<IAchievementInstance> GetAchievements(AchievementCategory? category = null);
        int GetTotalPoints();
        IReadOnlyList<PointMilestone> GetPointMilestones();
        bool ClaimMilestone(int milestoneIndex);
        void NotifyProgress(ConditionType type, string targetId, int amount);
    }
}
