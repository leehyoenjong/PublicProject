using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// UI 표시용 버프 데이터 인터페이스
    /// </summary>
    public interface IBuffUIData
    {
        string BuffId { get; }
        Sprite Icon { get; }
        BuffCategory Category { get; }
        float RemainingRatio { get; }
        string RemainingText { get; }
        int StackCount { get; }
        string TooltipTitle { get; }
        string TooltipDesc { get; }
    }
}
