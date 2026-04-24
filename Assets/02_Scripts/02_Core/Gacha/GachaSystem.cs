using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IGachaSystem 구현. 배너 노출 / 뽑기 자격 / 뽑기 처리의 단일 진입점.
    /// 재화 차감 및 보상 지급은 IInventorySystem, 추첨은 IDropResolver, 영속화는 IGachaRepository.
    /// </summary>
    public class GachaSystem : IGachaSystem
    {
        private readonly IEventBus _eventBus;
        private readonly IInventorySystem _inventory;
        private readonly IGachaRepository _repository;
        private readonly ITimeProvider _timeProvider;
        private IDropResolver _dropResolver;

        private readonly Dictionary<string, IBanner> _banners = new Dictionary<string, IBanner>();
        private readonly Dictionary<string, IGacha> _gachas = new Dictionary<string, IGacha>();
        private readonly Dictionary<string, string> _gachaToBanner = new Dictionary<string, string>();
        private readonly Dictionary<string, PityCounter> _pityCounters = new Dictionary<string, PityCounter>();

        public GachaSystem(IEventBus eventBus, IInventorySystem inventory, IGachaRepository repository, ITimeProvider timeProvider)
        {
            _eventBus = eventBus;
            _inventory = inventory;
            _repository = repository;
            _timeProvider = timeProvider;
            _dropResolver = new DefaultDropResolver();
            Debug.Log("[GachaSystem] Init started");
        }

        /// <summary>추첨 전략 교체(OCP). 미호출 시 DefaultDropResolver 사용.</summary>
        public void SetDropResolver(IDropResolver resolver)
        {
            if (resolver == null) return;
            _dropResolver = resolver;
            Debug.Log($"[GachaSystem] DropResolver replaced: {resolver.GetType().Name}");
        }

        public void Initialize(BannerDataCollection bannerCollection, GachaDataCollection gachaCollection)
        {
            Initialize(bannerCollection?.Items, gachaCollection?.Items);
        }

        /// <summary>테스트·DI 편의 오버로드. 인터페이스 컬렉션을 직접 주입.</summary>
        public void Initialize(IReadOnlyList<IBanner> banners, IReadOnlyList<IGacha> gachas)
        {
            _banners.Clear();
            _gachas.Clear();
            _gachaToBanner.Clear();

            if (gachas != null)
            {
                foreach (IGacha g in gachas)
                {
                    if (g == null || string.IsNullOrEmpty(g.MID)) continue;
                    _gachas[g.MID] = g;
                }
            }

            if (banners != null)
            {
                foreach (IBanner b in banners)
                {
                    if (b == null || string.IsNullOrEmpty(b.MID)) continue;
                    _banners[b.MID] = b;

                    if (b.Gachas == null) continue;
                    foreach (BannerGachaEntry entry in b.Gachas)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.GachaMID)) continue;
                        _gachaToBanner[entry.GachaMID] = b.MID;
                    }
                }
            }

            LoadPityCounters();

            Debug.Log($"[GachaSystem] Initialized — banners: {_banners.Count}, gachas: {_gachas.Count}, counters: {_pityCounters.Count}");
        }

        public IReadOnlyList<IBanner> GetVisibleBanners(IGachaContext context)
        {
            var list = new List<IBanner>();
            DateTime nowUtc = _timeProvider != null ? _timeProvider.NowUtc : DateTime.UtcNow;

            foreach (IBanner banner in _banners.Values)
            {
                if (!banner.IsActive) continue;
                if (!IsWithinBannerPeriod(banner, nowUtc)) continue;
                if (!IsBannerUnlocked(banner, context)) continue;

                list.Add(banner);
            }

            return list.AsReadOnly();
        }

        public IBanner GetBanner(string bannerMID)
        {
            _banners.TryGetValue(bannerMID, out IBanner banner);
            return banner;
        }

        public IGacha GetGacha(string gachaMID)
        {
            _gachas.TryGetValue(gachaMID, out IGacha gacha);
            return gacha;
        }

        public IPityCounter GetPityCounter(string gachaMID)
        {
            return EnsureCounter(gachaMID);
        }

        public PullEligibility CanPull(string gachaMID, int count, IGachaContext context)
        {
            if (count != 1 && count != 10)
            {
                return Fail("invalid_pull_count");
            }

            if (!_gachas.TryGetValue(gachaMID, out IGacha gacha))
            {
                return Fail("gacha_not_found");
            }

            if (!gacha.IsActive)
            {
                return Fail("inactive");
            }

            if (!IsGachaVisible(gachaMID, context, out string bannerBlock))
            {
                return Fail(bannerBlock);
            }

            int costItem = count == 10 ? EffectiveCost10Item(gacha) : gacha.Cost1Item;
            int costAmount = count == 10 ? EffectiveCost10Amount(gacha) : gacha.Cost1Amount;

            if (costItem == 0 || costAmount <= 0)
            {
                return Fail("cost_not_configured");
            }

            if (_inventory != null && _inventory.GetCount(costItem) < costAmount)
            {
                return Fail("insufficient_currency");
            }

            if (gacha.DailyLimit > 0 && _repository != null &&
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Daily) + count > gacha.DailyLimit)
            {
                return Fail("daily_limit_exceeded");
            }

            if (gacha.PeriodLimit > 0 && _repository != null &&
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Period) + count > gacha.PeriodLimit)
            {
                return Fail("period_limit_exceeded");
            }

            if (gacha.LifetimeLimit > 0 && _repository != null &&
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Lifetime) + count > gacha.LifetimeLimit)
            {
                return Fail("lifetime_limit_exceeded");
            }

            return new PullEligibility { CanPull = true, BlockReason = null };
        }

        public void Pull(string gachaMID, int count, IGachaContext context, Action<PullResult> callback)
        {
            PullEligibility eligibility = CanPull(gachaMID, count, context);
            if (!eligibility.CanPull)
            {
                FailPull(gachaMID, count, eligibility.BlockReason, callback);
                return;
            }

            IGacha gacha = _gachas[gachaMID];

            _eventBus?.Publish(new GachaPullRequestedEvent
            {
                GachaMID = gachaMID,
                Count = count,
                PaymentType = gacha.PaymentType
            });

            int costItem = count == 10 ? EffectiveCost10Item(gacha) : gacha.Cost1Item;
            int costAmount = count == 10 ? EffectiveCost10Amount(gacha) : gacha.Cost1Amount;

            if (_inventory != null && !_inventory.ConsumeByMID(costItem, costAmount))
            {
                FailPull(gachaMID, count, "currency_consume_failed", callback);
                return;
            }

            PityCounter counter = EnsureCounter(gachaMID);
            PityCounterState state = counter.ToState();

            IReadOnlyList<GachaRollResult> rolls = _dropResolver.Resolve(gacha, state, count);
            counter.FromState(state);

            var rewards = new List<GachaRewardItem>(rolls.Count);
            bool hardPityHit = false;
            bool pickupPityHit = false;

            foreach (GachaRollResult roll in rolls)
            {
                GachaRewardItem reward = GrantItem(roll);
                rewards.Add(reward);
                bool wasPickup = roll.TriggeredPickupPity;
                counter.ApplyRoll(roll.Tier, wasPickup);
                if (roll.TriggeredHardPity) hardPityHit = true;
                if (roll.TriggeredPickupPity) pickupPityHit = true;
            }

            long nowUnix = _timeProvider != null ? _timeProvider.NowUnixSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            counter.SetLastPullAt(nowUnix);
            _repository?.Save(counter);

            IncrementPurchaseCounts(gachaMID, count);

            GachaPullSummary summary = BuildSummary(gacha, rewards, hardPityHit, pickupPityHit, count);

            if (hardPityHit || pickupPityHit)
            {
                _eventBus?.Publish(new GachaPityTriggeredEvent
                {
                    GachaMID = gachaMID,
                    HardPity = hardPityHit,
                    PickupPity = pickupPityHit,
                    PullCountAtTrigger = counter.TotalPullCount
                });
            }

            string bannerMID = _gachaToBanner.TryGetValue(gachaMID, out string b) ? b : null;
            _eventBus?.Publish(new GachaPullCompletedEvent
            {
                GachaMID = gachaMID,
                BannerMID = bannerMID,
                Count = count,
                Rewards = rewards,
                Summary = summary
            });

            Debug.Log($"[GachaSystem] Pull completed: {gachaMID} x{count} (SSR:{summary.SSRCount} SR:{summary.SRCount} R:{summary.RCount})");

            callback?.Invoke(new PullResult
            {
                Success = true,
                GachaMID = gachaMID,
                FailureReason = null,
                Rewards = rewards,
                Summary = summary
            });
        }

        private GachaRewardItem GrantItem(GachaRollResult roll)
        {
            ItemAddResult addResult = _inventory != null
                ? _inventory.AddItem(roll.ItemMID, 1, "Gacha")
                : default;

            bool wasConverted = addResult.ConvertedItems != null && addResult.ConvertedItems.Count > 0;
            int finalMID = wasConverted ? addResult.ConvertedItems[0].MID : roll.ItemMID;
            int count = wasConverted ? addResult.ConvertedItems[0].Count : addResult.AddedCount;

            return new GachaRewardItem
            {
                OriginalItemMID = roll.ItemMID,
                FinalItemMID = finalMID,
                Count = count > 0 ? count : 1,
                Tier = roll.Tier,
                IsDuplicate = wasConverted,
                WasConverted = wasConverted,
                IsNew = !wasConverted
            };
        }

        private GachaPullSummary BuildSummary(IGacha gacha, List<GachaRewardItem> rewards, bool hardPity, bool pickupPity, int originalCount)
        {
            int ssr = 0, sr = 0, r = 0, n = 0;
            foreach (GachaRewardItem item in rewards)
            {
                switch (item.Tier)
                {
                    case GachaTierRank.SSR: ssr++; break;
                    case GachaTierRank.SR: sr++; break;
                    case GachaTierRank.R: r++; break;
                    default: n++; break;
                }
            }

            bool guaranteedApplied = originalCount == 10 && gacha.BonusGuaranteedTier != GuaranteedTier.None;
            bool bonus11thApplied = originalCount == 10 && gacha.Bonus11th;

            return new GachaPullSummary
            {
                PullCount = rewards.Count,
                SSRCount = ssr,
                SRCount = sr,
                RCount = r,
                NCount = n,
                HardPityTriggered = hardPity,
                PickupPityTriggered = pickupPity,
                GuaranteedBonusApplied = guaranteedApplied,
                Bonus11thApplied = bonus11thApplied
            };
        }

        private void IncrementPurchaseCounts(string gachaMID, int count)
        {
            if (_repository == null) return;

            _repository.SetPurchaseCount(gachaMID, PurchaseScope.Daily,
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Daily) + count);
            _repository.SetPurchaseCount(gachaMID, PurchaseScope.Period,
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Period) + count);
            _repository.SetPurchaseCount(gachaMID, PurchaseScope.Lifetime,
                _repository.GetPurchaseCount(gachaMID, PurchaseScope.Lifetime) + count);
        }

        private void FailPull(string gachaMID, int count, string reason, Action<PullResult> callback)
        {
            _eventBus?.Publish(new GachaPullFailedEvent
            {
                GachaMID = gachaMID,
                Count = count,
                Reason = reason
            });

            Debug.Log($"[GachaSystem] Pull failed: {gachaMID} x{count} (reason: {reason})");
            callback?.Invoke(new PullResult
            {
                Success = false,
                GachaMID = gachaMID,
                FailureReason = reason,
                Rewards = null,
                Summary = default
            });
        }

        private bool IsWithinBannerPeriod(IBanner banner, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(banner.PeriodStartUtc) && string.IsNullOrEmpty(banner.PeriodEndUtc))
            {
                return true;
            }

            DateTime start = TryParseUtc(banner.PeriodStartUtc, DateTime.MinValue);
            DateTime end = TryParseUtc(banner.PeriodEndUtc, DateTime.MaxValue);
            return nowUtc >= start && nowUtc < end;
        }

        private bool IsBannerUnlocked(IBanner banner, IGachaContext context)
        {
            if (banner.UnlockType == BannerUnlockType.None) return true;
            if (context == null) return false;

            switch (banner.UnlockType)
            {
                case BannerUnlockType.MinLevel:
                    return int.TryParse(banner.UnlockValue, out int minLevel) && context.PlayerLevel >= minLevel;
                case BannerUnlockType.QuestClear:
                    return int.TryParse(banner.UnlockValue, out int questMID) && context.IsQuestCleared(questMID);
                default:
                    return true;
            }
        }

        private bool IsGachaVisible(string gachaMID, IGachaContext context, out string blockReason)
        {
            if (!_gachaToBanner.TryGetValue(gachaMID, out string bannerMID))
            {
                blockReason = null;
                return true;
            }

            if (!_banners.TryGetValue(bannerMID, out IBanner banner))
            {
                blockReason = null;
                return true;
            }

            DateTime nowUtc = _timeProvider != null ? _timeProvider.NowUtc : DateTime.UtcNow;

            if (!banner.IsActive)
            {
                blockReason = "banner_inactive";
                return false;
            }

            if (!IsWithinBannerPeriod(banner, nowUtc))
            {
                blockReason = "banner_out_of_period";
                return false;
            }

            if (!IsBannerUnlocked(banner, context))
            {
                blockReason = "banner_locked";
                return false;
            }

            blockReason = null;
            return true;
        }

        private PityCounter EnsureCounter(string gachaMID)
        {
            if (!_pityCounters.TryGetValue(gachaMID, out PityCounter counter))
            {
                counter = new PityCounter(gachaMID);
                _pityCounters[gachaMID] = counter;
            }
            return counter;
        }

        private void LoadPityCounters()
        {
            _pityCounters.Clear();
            if (_repository == null) return;

            IReadOnlyList<IPityCounter> saved = _repository.LoadAll();
            if (saved == null) return;

            foreach (IPityCounter c in saved)
            {
                if (c == null || string.IsNullOrEmpty(c.GachaMID)) continue;
                _pityCounters[c.GachaMID] = new PityCounter(c.GachaMID, c.PullsSinceLastSSR, c.PullsSinceLastPickup, c.TotalPullCount, c.LastPullAtUtc);
            }
        }

        private int EffectiveCost10Item(IGacha gacha)
        {
            return gacha.Cost10Item > 0 ? gacha.Cost10Item : gacha.Cost1Item;
        }

        private int EffectiveCost10Amount(IGacha gacha)
        {
            return gacha.Cost10Amount > 0 ? gacha.Cost10Amount : gacha.Cost1Amount * 10;
        }

        private static PullEligibility Fail(string reason)
        {
            return new PullEligibility { CanPull = false, BlockReason = reason };
        }

        private static DateTime TryParseUtc(string raw, DateTime fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                return parsed;
            }
            return fallback;
        }
    }
}
