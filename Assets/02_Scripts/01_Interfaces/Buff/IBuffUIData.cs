namespace PublicFramework
{
    /// <summary>
    /// UI 표시용 버프 데이터 인터페이스. 아이콘은 버프 자체가 아닌 적용 소스(스킬/장비 등) 쪽에서 제공한다.
    /// </summary>
    public interface IBuffUIData
    {
        string BuffId { get; }
        BuffCategory Category { get; }
        float RemainingRatio { get; }
        string RemainingText { get; }
        int StackCount { get; }
        string TooltipTitle { get; }
        string TooltipDesc { get; }
    }
}
