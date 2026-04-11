namespace PublicFramework
{
    /// <summary>
    /// UI용 조건 진행 데이터 인터페이스
    /// </summary>
    public interface IConditionProgress
    {
        string ConditionId { get; }
        ConditionType ConditionType { get; }
        string Description { get; }
        int CurrentAmount { get; }
        int RequiredAmount { get; }
        float Progress { get; }
        bool IsCompleted { get; }
    }
}
