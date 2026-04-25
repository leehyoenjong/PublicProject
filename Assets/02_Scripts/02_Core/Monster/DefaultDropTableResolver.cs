using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 기본 드롭 해석기. 항목별 독립 확률(weight = 0~100 percent) 로 처리.
    /// minPlayerLevel 미만이면 스킵, repeatLimit 도달 시 스킵, weight ≥ 100 은 항상 드롭.
    /// </summary>
    public class DefaultDropTableResolver : IDropTableResolver
    {
        public DropResult Resolve(IDropTable table, IDropContext context, IRandomProvider random)
        {
            var drops = new List<DropItemResult>();
            if (table == null || table.Entries == null) return new DropResult { Drops = drops };

            int playerLevel = context?.PlayerLevel ?? 0;

            foreach (IDropEntry entry in table.Entries)
            {
                if (entry == null || entry.Weight <= 0) continue;
                if (entry.MinPlayerLevel > 0 && playerLevel < entry.MinPlayerLevel) continue;

                if (entry.RepeatLimit > 0 && context != null
                    && context.GetDropCount(entry.ItemMID) >= entry.RepeatLimit) continue;

                int roll = random.NextInt(100);
                if (roll >= entry.Weight) continue;

                int min = entry.MinCount < 1 ? 1 : entry.MinCount;
                int max = entry.MaxCount < min ? min : entry.MaxCount;
                int count = min == max ? min : random.NextInt(min, max + 1);

                drops.Add(new DropItemResult { ItemMID = entry.ItemMID, Count = count });
            }

            return new DropResult { Drops = drops };
        }
    }
}
