using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 런타임 버프 인스턴스 인터페이스
    /// </summary>
    public interface IBuffInstance
    {
        string BuffId { get; }
        string TargetId { get; }
        string SourceId { get; }
        BuffCategory Category { get; }
        int CurrentStack { get; }
        int MaxStack { get; }
        float RemainingDuration { get; }
        bool IsExpired { get; }
        bool IsUndispellable { get; }
        IReadOnlyList<IStatModifier> Modifiers { get; }
    }
}
