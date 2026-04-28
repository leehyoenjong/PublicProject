namespace PublicFramework
{
    public enum BehaviorNodeStatus
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// BT 노드 종류. Composite/Decorator 는 type enum 으로 내장 처리, Action/Condition 만 Registry lookup.
    /// </summary>
    public enum BehaviorNodeType
    {
        Sequence,
        Selector,
        Inverter,
        Cooldown,
        Repeat,
        Condition,
        Action
    }
}
