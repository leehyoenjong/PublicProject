using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 소프트 천장 포함 뽑기 전략.
    /// softPityStart 이후 뽑기마다 최고 등급 확률이 증가한다.
    /// PickupGuarantee: 최고 등급 비픽업 획득 시 다음 최고 등급은 픽업 확정.
    /// </summary>
    public class SoftPityPullStrategy : IPullStrategy
    {
        private readonly int _softPityStart;
        private readonly int _hardPityCount;
        private readonly float _softPityRateIncrease;

        public SoftPityPullStrategy(int softPityStart, int hardPityCount, float softPityRateIncrease)
        {
            _softPityStart = softPityStart;
            _hardPityCount = hardPityCount;
            _softPityRateIncrease = softPityRateIncrease;
        }

        public GachaReward[] Pull(DropTable dropTable, PityCounter pityCounter, int count)
        {
            if (dropTable.Entries == null || dropTable.Entries.Count == 0)
            {
                Debug.LogWarning("[SoftPityPull] Empty drop table");
                return System.Array.Empty<GachaReward>();
            }

            var rewards = new GachaReward[count];
            IReadOnlyList<DropEntry> entries = dropTable.Entries;
            ItemGrade highestGrade = GetHighestGrade(entries);

            for (int i = 0; i < count; i++)
            {
                pityCounter.PullCount++;

                // 하드 천장 도달
                if (_hardPityCount > 0 && pityCounter.PullCount >= _hardPityCount)
                {
                    DropEntry pityEntry = ResolvePickupGuarantee(entries, pityCounter, highestGrade);
                    rewards[i] = CreateReward(pityEntry);
                    pityCounter.PullCount = 0;
                    Debug.Log("[SoftPityPull] Hard pity reached — guaranteed highest grade");
                    continue;
                }

                // 소프트 천장 확률 보정
                float bonusRate = GetSoftPityBonus(pityCounter.PullCount);
                DropEntry selected = RollWithPityBonus(entries, bonusRate);
                rewards[i] = CreateReward(selected);

                // 최고 등급 획득 시 PickupGuarantee 처리 + 카운터 리셋
                if (selected.Grade == highestGrade)
                {
                    rewards[i] = CreateReward(ResolvePickupGuarantee(entries, pityCounter, highestGrade, selected));
                    pityCounter.PullCount = 0;
                }
            }

            Debug.Log($"[SoftPityPull] Pulled {count} items (pity: {pityCounter.PullCount})");
            return rewards;
        }

        public float GetProbability(DropTable dropTable, PityCounter pityCounter, ItemGrade grade)
        {
            if (_hardPityCount > 0 && pityCounter.PullCount >= _hardPityCount - 1)
            {
                ItemGrade highest = GetHighestGrade(dropTable.Entries);
                return grade == highest ? 1f : 0f;
            }

            int totalWeight = 0;
            int gradeWeight = 0;
            ItemGrade highestGrade = GetHighestGrade(dropTable.Entries);

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

            float bonusRate = GetSoftPityBonus(pityCounter.PullCount);
            bonusRate = Mathf.Min(bonusRate, 1f);

            float baseProb = (float)gradeWeight / totalWeight;

            if (bonusRate > 0f)
            {
                if (grade == highestGrade)
                {
                    baseProb = baseProb + bonusRate * (1f - baseProb);
                }
                else
                {
                    baseProb = baseProb * (1f - bonusRate);
                }
            }

            return baseProb;
        }

        private DropEntry ResolvePickupGuarantee(IReadOnlyList<DropEntry> entries,
            PityCounter pityCounter, ItemGrade highestGrade, DropEntry rolled = null)
        {
            bool hasPickupItems = HasPickupEntries(entries, highestGrade);
            if (!hasPickupItems)
            {
                return rolled ?? GetHighestGradeEntry(entries);
            }

            // IsGuaranteed == true → 픽업 아이템 강제 선택
            if (pityCounter.IsGuaranteed)
            {
                pityCounter.IsGuaranteed = false;
                DropEntry pickup = GetPickupEntry(entries, highestGrade);
                Debug.Log("[SoftPityPull] Pickup guaranteed — forced pickup item");
                return pickup;
            }

            // 50/50 판정
            if (rolled == null)
            {
                rolled = GetHighestGradeEntry(entries);
            }

            if (rolled.IsPickup)
            {
                // 픽업 아이템 획득 → OK
                return rolled;
            }

            // 비픽업 획득 → 다음 최고 등급은 픽업 확정
            pityCounter.IsGuaranteed = true;
            Debug.Log("[SoftPityPull] Non-pickup obtained — next highest grade is guaranteed pickup");
            return rolled;
        }

        private float GetSoftPityBonus(int pullCount)
        {
            if (pullCount < _softPityStart) return 0f;

            int overCount = pullCount - _softPityStart;
            return overCount * _softPityRateIncrease;
        }

        private DropEntry RollWithPityBonus(IReadOnlyList<DropEntry> entries, float bonusRate)
        {
            if (bonusRate > 0f)
            {
                float roll = Random.value;
                if (roll < bonusRate)
                {
                    return GetHighestGradeEntry(entries);
                }
            }

            return RollWeighted(entries);
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

        private GachaReward GetHighestGradeReward(IReadOnlyList<DropEntry> entries)
        {
            DropEntry highest = GetHighestGradeEntry(entries);
            return CreateReward(highest);
        }

        private DropEntry GetHighestGradeEntry(IReadOnlyList<DropEntry> entries)
        {
            DropEntry highest = entries[0];
            foreach (DropEntry entry in entries)
            {
                if (entry.Grade > highest.Grade)
                {
                    highest = entry;
                }
            }
            return highest;
        }

        private ItemGrade GetHighestGrade(IReadOnlyList<DropEntry> entries)
        {
            ItemGrade highest = ItemGrade.Common;
            foreach (DropEntry entry in entries)
            {
                if (entry.Grade > highest)
                {
                    highest = entry.Grade;
                }
            }
            return highest;
        }

        private bool HasPickupEntries(IReadOnlyList<DropEntry> entries, ItemGrade grade)
        {
            foreach (DropEntry entry in entries)
            {
                if (entry.IsPickup && entry.Grade == grade)
                {
                    return true;
                }
            }
            return false;
        }

        private DropEntry GetPickupEntry(IReadOnlyList<DropEntry> entries, ItemGrade grade)
        {
            var pickups = new List<DropEntry>();
            foreach (DropEntry entry in entries)
            {
                if (entry.IsPickup && entry.Grade == grade)
                {
                    pickups.Add(entry);
                }
            }

            if (pickups.Count == 0)
            {
                return GetHighestGradeEntry(entries);
            }

            // 복수 픽업 아이템 시 가중치 랜덤
            if (pickups.Count == 1) return pickups[0];

            int totalWeight = 0;
            foreach (DropEntry entry in pickups)
            {
                totalWeight += GetEffectiveWeight(entry);
            }

            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;

            foreach (DropEntry entry in pickups)
            {
                accumulated += GetEffectiveWeight(entry);
                if (roll < accumulated)
                {
                    return entry;
                }
            }

            return pickups[pickups.Count - 1];
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
