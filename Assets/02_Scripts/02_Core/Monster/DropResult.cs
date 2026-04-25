using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>드롭 계산 결과. 항목 0개도 가능(전부 확률 미달).</summary>
    public struct DropResult
    {
        public IReadOnlyList<DropItemResult> Drops;
    }

    /// <summary>드롭 결과의 단일 항목.</summary>
    public struct DropItemResult
    {
        public int ItemMID;
        public int Count;
    }
}
