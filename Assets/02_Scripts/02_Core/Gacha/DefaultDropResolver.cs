using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 2단 추첨 구현. Tier → Item 순으로 weight 누적 롤.
    /// 천장: Hard 카운터 도달 시 SSR 강제, Pickup 카운터 도달 시 SSR + Pickup 아이템 강제.
    /// 10연 보너스(bonus11th / bonusGuaranteedTier)는 시스템 레이어가 아닌 Resolver 레이어에서 해석.
    /// </summary>
    public class DefaultDropResolver : IDropResolver
    {
        public IReadOnlyList<GachaRollResult> Resolve(IGacha gacha, PityCounterState pityCounter, int count)
        {
            int effectiveCount = count == 10 && gacha.Bonus11th ? 11 : count;
            var results = new List<GachaRollResult>(effectiveCount);

            for (int i = 0; i < effectiveCount; i++)
            {
                GachaRollResult rolled = RollOne(gacha, pityCounter);
                ApplyCounterMutation(rolled, pityCounter);
                results.Add(rolled);
            }

            if (count == 10)
            {
                ApplyGuaranteedTierBonus(gacha, results);
            }

            return results;
        }

        private GachaRollResult RollOne(IGacha gacha, PityCounterState counter)
        {
            bool pickupPityReached = gacha.PityPickupCount > 0 && counter.PullsSinceLastPickup + 1 >= gacha.PityPickupCount;
            bool hardPityReached = gacha.PityHardCount > 0 && counter.PullsSinceLastSSR + 1 >= gacha.PityHardCount;

            if (pickupPityReached)
            {
                int pickupItem = PickSSRItemWeighted(gacha);
                return new GachaRollResult
                {
                    Tier = GachaTierRank.SSR,
                    ItemMID = pickupItem,
                    TriggeredHardPity = false,
                    TriggeredPickupPity = true
                };
            }

            if (hardPityReached)
            {
                int ssrItem = PickSSRItemWeighted(gacha);
                return new GachaRollResult
                {
                    Tier = GachaTierRank.SSR,
                    ItemMID = ssrItem,
                    TriggeredHardPity = true,
                    TriggeredPickupPity = false
                };
            }

            GachaTierRank tier = RollTier(gacha);
            int itemMID = RollItem(gacha, tier);

            return new GachaRollResult
            {
                Tier = tier,
                ItemMID = itemMID,
                TriggeredHardPity = false,
                TriggeredPickupPity = false
            };
        }

        private void ApplyCounterMutation(GachaRollResult rolled, PityCounterState counter)
        {
            counter.PullsSinceLastSSR = rolled.Tier == GachaTierRank.SSR ? 0 : counter.PullsSinceLastSSR + 1;
            counter.PullsSinceLastPickup = rolled.TriggeredPickupPity ? 0 : counter.PullsSinceLastPickup + 1;
        }

        private GachaTierRank RollTier(IGacha gacha)
        {
            int totalWeight = 0;
            foreach (GachaTierEntry entry in gacha.Tiers)
            {
                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning($"[가챠] 티어 가중치 합이 0임: {gacha.MID}");
                return GachaTierRank.N;
            }

            int roll = Random.Range(0, totalWeight);
            int acc = 0;
            foreach (GachaTierEntry entry in gacha.Tiers)
            {
                acc += entry.Weight;
                if (roll < acc) return entry.Tier;
            }

            return gacha.Tiers[gacha.Tiers.Count - 1].Tier;
        }

        private int RollItem(IGacha gacha, GachaTierRank tier)
        {
            int totalWeight = 0;
            foreach (GachaDropEntry entry in gacha.Drops)
            {
                if (entry.Tier == tier) totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning($"[가챠] 드롭 항목 없음: 티어={tier}, 가챠={gacha.MID}");
                return 0;
            }

            int roll = Random.Range(0, totalWeight);
            int acc = 0;
            foreach (GachaDropEntry entry in gacha.Drops)
            {
                if (entry.Tier != tier) continue;
                acc += entry.Weight;
                if (roll < acc) return entry.ItemMID;
            }

            return 0;
        }

        private int PickSSRItemWeighted(IGacha gacha)
        {
            return RollItem(gacha, GachaTierRank.SSR);
        }

        /// <summary>
        /// 10연 확정 등급 보너스. 전체 결과에 bonusGuaranteedTier 이상이 하나도 없으면 마지막 슬롯을 강제 승격.
        /// </summary>
        private void ApplyGuaranteedTierBonus(IGacha gacha, List<GachaRollResult> results)
        {
            if (gacha.BonusGuaranteedTier == GuaranteedTier.None) return;

            GachaTierRank minTier = GuaranteedToTierRank(gacha.BonusGuaranteedTier);

            foreach (GachaRollResult r in results)
            {
                if (r.Tier >= minTier) return;
            }

            int itemMID = RollItem(gacha, minTier);
            if (itemMID == 0) return;

            int lastIndex = results.Count - 1;
            GachaRollResult old = results[lastIndex];
            results[lastIndex] = new GachaRollResult
            {
                Tier = minTier,
                ItemMID = itemMID,
                TriggeredHardPity = old.TriggeredHardPity,
                TriggeredPickupPity = old.TriggeredPickupPity
            };
        }

        private GachaTierRank GuaranteedToTierRank(GuaranteedTier tier)
        {
            switch (tier)
            {
                case GuaranteedTier.N: return GachaTierRank.N;
                case GuaranteedTier.R: return GachaTierRank.R;
                case GuaranteedTier.SR: return GachaTierRank.SR;
                case GuaranteedTier.SSR: return GachaTierRank.SSR;
                default: return GachaTierRank.N;
            }
        }
    }
}
