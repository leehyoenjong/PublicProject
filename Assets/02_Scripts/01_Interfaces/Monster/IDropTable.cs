using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>드롭 테이블 정의 (MID 단위로 묶인 항목 집합).</summary>
    public interface IDropTable
    {
        string MID { get; }
        IReadOnlyList<IDropEntry> Entries { get; }
    }

    /// <summary>드롭 테이블의 단일 항목.</summary>
    public interface IDropEntry
    {
        int Order { get; }
        int ItemMID { get; }
        int Weight { get; }
        int MinCount { get; }
        int MaxCount { get; }
        int MinPlayerLevel { get; }
        int RepeatLimit { get; }
    }
}
