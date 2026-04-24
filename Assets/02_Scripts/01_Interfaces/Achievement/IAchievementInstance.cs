using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 업적 인스턴스 인터페이스
    /// </summary>
    public interface IAchievementInstance
    {
        string AchievementId { get; }
        int DisplayName { get; }
        int Description { get; }
        AchievementCategory Category { get; }
        AchievementState State { get; }
        bool IsHidden { get; }
        int CurrentTier { get; }
        int MaxTier { get; }
        float Progress { get; }
        int CurrentAmount { get; }
        int RequiredAmount { get; }
        IReadOnlyList<AchievementTierData> GetTiers();
    }
}
