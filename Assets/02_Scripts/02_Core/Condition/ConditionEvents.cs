namespace PublicFramework
{
    /// <summary>
    /// 범용 조건 진행 이벤트.
    /// Kill/Collect/LevelReach/StageClear/Login/PlayTime 등 개별 시스템에서 발행하여
    /// QuestTracker/AchievementTracker가 구독한다.
    /// </summary>
    public struct ConditionProgressEvent
    {
        public ConditionType Type;
        public string TargetId;
        public int Amount;
    }
}
