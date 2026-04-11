namespace PublicFramework
{
    /// <summary>
    /// 조건 인터페이스. 퀘스트/업적이 공유한다.
    /// </summary>
    public interface ICondition
    {
        string ConditionId { get; }
        ConditionType ConditionType { get; }
        string TargetId { get; }
        int RequiredAmount { get; }
        int CurrentAmount { get; }
        bool IsCompleted { get; }
        float Progress { get; }
        void AddProgress(int amount);
        void Reset();
    }
}
