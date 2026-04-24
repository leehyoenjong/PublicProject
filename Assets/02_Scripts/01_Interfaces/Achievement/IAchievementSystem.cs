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
        void NotifyProgress(ConditionType type, string targetId, int amount);

        /// <summary>
        /// 현재까지 Claim 된 티어의 보유효과를 전부 집계해 반환.
        /// 캐릭터 스탯 계산 시 StatSystem 에서 StatModifier 로 변환해 주입.
        /// </summary>
        IReadOnlyList<PassiveStat> GetActivePassiveStats();
    }
}
