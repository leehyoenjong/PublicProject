namespace PublicFramework
{
    /// <summary>
    /// BT Action / Condition 노드 한 종류. actionKey 로 BehaviorActionRegistry 에 등록.
    /// Tick 은 NodeStatus 반환 (Success/Failure/Running).
    /// param1~3 은 actionKey 별 의미가 다름 (SkillActionEntry 와 동일 패턴).
    /// </summary>
    public interface IBehaviorAction
    {
        string ActionKey { get; }
        BehaviorNodeStatus Tick(BehaviorContext context, string param1, string param2, string param3);
    }
}
