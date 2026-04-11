using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 업적 인스턴스 인터페이스
    /// </summary>
    public interface IAchievementInstance
    {
        string AchievementId { get; }
        string DisplayName { get; }
        string Description { get; }
        AchievementCategory Category { get; }
        AchievementState State { get; }
        bool IsHidden { get; }
        int CurrentTier { get; }
        int MaxTier { get; }
        float Progress { get; }
        int CurrentAmount { get; }
        int RequiredAmount { get; }
        int TotalPoints { get; }
        IReadOnlyList<AchievementTierData> GetTiers();
    }
}
