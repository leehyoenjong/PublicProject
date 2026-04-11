using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 가중치 뽑기 전략. 천장 기능 없음. PityType.None 배너에 사용.
    /// </summary>
    public class WeightedPullStrategy : IPullStrategy
    {
        public GachaReward[] Pull(DropTable dropTable, PityCounter pityCounter, int count)
        {
            if (dropTable.Entries == null || dropTable.Entries.Count == 0)
            {
                Debug.LogWarning("[WeightedPull] Empty drop table");
                return System.Array.Empty<GachaReward>();
            }

            var rewards = new GachaReward[count];
            IReadOnlyList<DropEntry> entries = dropTable.Entries;

            for (int i = 0; i < count; i++)
            {
                DropEntry selected = RollWeighted(entries);
                rewards[i] = CreateReward(selected);
                pityCounter.PullCount++;
            }

            Debug.Log($"[WeightedPull] Pulled {count} items");
            return rewards;
        }

        public float GetProbability(DropTable dropTable, PityCounter pityCounter, ItemGrade grade)
        {
            int totalWeight = 0;
            int gradeWeight = 0;

            foreach (DropEntry entry in dropTable.Entries)
            {
                int w = GetEffectiveWeight(entry);
                totalWeight += w;
                if (entry.Grade == grade)
                {
                    gradeWeight += w;
                }
            }

            if (totalWeight <= 0) return 0f;
            return (float)gradeWeight / totalWeight;
        }

        private int GetEffectiveWeight(DropEntry entry)
        {
            return entry.IsPickup ? entry.Weight + entry.PickupWeight : entry.Weight;
        }

        private DropEntry RollWeighted(IReadOnlyList<DropEntry> entries)
        {
            int totalWeight = 0;
            foreach (DropEntry entry in entries)
            {
                totalWeight += GetEffectiveWeight(entry);
            }

            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;

            foreach (DropEntry entry in entries)
            {
                accumulated += GetEffectiveWeight(entry);
                if (roll < accumulated)
                {
                    return entry;
                }
            }

            return entries[entries.Count - 1];
        }

        private GachaReward CreateReward(DropEntry entry)
        {
            return new GachaReward
            {
                RewardId = entry.ItemId,
                RewardType = entry.ItemType,
                Grade = entry.Grade,
                Amount = 1,
                IsNew = false,
                IsDuplicate = false
            };
        }
    }
}
