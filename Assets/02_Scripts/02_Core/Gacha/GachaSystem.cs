using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IGachaSystem 구현체 — 배너 관리, Pull 처리, 천장 계산
    /// IEventBus, ISaveSystem 생성자 주입 (DIP)
    /// </summary>
    public class GachaSystem : IGachaSystem
    {
        private const string PITY_SAVE_PREFIX = "gacha_pity_";
        private const int SAVE_SLOT = 0;

        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<string, GachaBannerData> _banners = new Dictionary<string, GachaBannerData>();
        private readonly Dictionary<string, IPullStrategy> _strategies = new Dictionary<string, IPullStrategy>();
        private readonly Dictionary<string, PityCounter> _pityCounters = new Dictionary<string, PityCounter>();
        private readonly IPullStrategy _defaultStrategy;
        private IDuplicateHandler _duplicateHandler;

        public GachaSystem(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _defaultStrategy = new WeightedPullStrategy();

            Debug.Log("[GachaSystem] Init started");
        }

        public void SetDuplicateHandler(IDuplicateHandler handler)
        {
            _duplicateHandler = handler;
        }

        public GachaResult Pull(string bannerId, int count = 1)
        {
            if (!_banners.TryGetValue(bannerId, out GachaBannerData banner))
            {
                Debug.LogError($"[GachaSystem] Banner not found: {bannerId}");
                return new GachaResult { Success = false, FailReason = "Banner not found" };
            }

            if (banner.DropTable == null)
            {
                Debug.LogError($"[GachaSystem] DropTable is null for banner: {bannerId}");
                return new GachaResult { Success = false, FailReason = "DropTable is null" };
            }

            if (banner.DropTable.Entries == null || banner.DropTable.Entries.Count == 0)
            {
                Debug.LogError($"[GachaSystem] DropTable entries empty for banner: {bannerId}");
                return new GachaResult { Success = false, FailReason = "DropTable entries empty" };
            }

            if (count <= 0)
            {
                Debug.LogWarning($"[GachaSystem] Invalid pull count: {count}");
                return new GachaResult { Success = false, FailReason = "Invalid pull count" };
            }

            PityCounter pityCounter = GetOrCreatePityCounter(bannerId);
            IPullStrategy strategy = GetStrategy(bannerId);

            _eventBus?.Publish(new GachaPullStartEvent
            {
                BannerId = bannerId,
                Count = count
            });

            int previousPullCount = pityCounter.PullCount;

            // 뽑기 실행
            GachaReward[] rewards = strategy.Pull(banner.DropTable, pityCounter, count);

            // Multi 보장 처리
            if (count > 1 && count >= banner.MultiPullCount)
            {
                rewards = ApplyMultiGuarantee(rewards, banner);
            }

            // 중복 처리
            rewards = ProcessDuplicates(rewards, banner);

            // 천장 리셋 감지
            if (previousPullCount > 0 && pityCounter.PullCount < previousPullCount)
            {
                _eventBus?.Publish(new GachaPityReachedEvent
                {
                    BannerId = bannerId,
                    PityType = banner.PityType,
                    PullCount = previousPullCount + count
                });

                _eventBus?.Publish(new GachaPityResetEvent
                {
                    BannerId = bannerId,
                    PreviousPullCount = previousPullCount
                });
            }

            pityCounter.LastPullTime = DateTime.Now;
            SavePityCounter(pityCounter);

            _eventBus?.Publish(new GachaPullResultEvent
            {
                BannerId = bannerId,
                Rewards = rewards,
                TotalPullCount = pityCounter.PullCount
            });

            Debug.Log($"[GachaSystem] Pull complete: {bannerId} x{count} (pity: {pityCounter.PullCount})");

            return new GachaResult
            {
                Success = true,
                Rewards = rewards,
                PityInfo = pityCounter
            };
        }

        public GachaBannerData GetBannerInfo(string bannerId)
        {
            _banners.TryGetValue(bannerId, out GachaBannerData banner);
            return banner;
        }

        public IReadOnlyList<GachaBannerData> GetActiveBanners()
        {
            var active = new List<GachaBannerData>();

            foreach (GachaBannerData banner in _banners.Values)
            {
                if (IsBannerActive(banner))
                {
                    active.Add(banner);
                }
            }

            return active.AsReadOnly();
        }

        public PityCounter GetPityInfo(string bannerId)
        {
            return GetOrCreatePityCounter(bannerId);
        }

        public IReadOnlyList<DropEntry> GetProbabilities(string bannerId)
        {
            if (!_banners.TryGetValue(bannerId, out GachaBannerData banner) || banner.DropTable == null)
            {
                return new List<DropEntry>().AsReadOnly();
            }

            return banner.DropTable.Entries;
        }

        public void RegisterBanner(GachaBannerData bannerData)
        {
            if (bannerData == null)
            {
                Debug.LogError("[GachaSystem] BannerData is null");
                return;
            }

            _banners[bannerData.BannerId] = bannerData;

            // PityType에 따라 자동으로 적절한 전략 설정
            if (!_strategies.ContainsKey(bannerData.BannerId))
            {
                AssignDefaultStrategy(bannerData);
            }

            _eventBus?.Publish(new GachaBannerOpenEvent
            {
                BannerId = bannerData.BannerId,
                BannerType = bannerData.BannerType
            });

            Debug.Log($"[GachaSystem] Banner registered: {bannerData.BannerId}");
        }

        public void UnregisterBanner(string bannerId)
        {
            if (_banners.Remove(bannerId))
            {
                _strategies.Remove(bannerId);

                _eventBus?.Publish(new GachaBannerCloseEvent
                {
                    BannerId = bannerId
                });

                Debug.Log($"[GachaSystem] Banner unregistered: {bannerId}");
            }
        }

        public void SetPullStrategy(string bannerId, IPullStrategy strategy)
        {
            _strategies[bannerId] = strategy;
            Debug.Log($"[GachaSystem] Strategy set for: {bannerId}");
        }

        private IPullStrategy GetStrategy(string bannerId)
        {
            if (_strategies.TryGetValue(bannerId, out IPullStrategy strategy))
            {
                return strategy;
            }
            return _defaultStrategy;
        }

        private PityCounter GetOrCreatePityCounter(string bannerId)
        {
            if (_pityCounters.TryGetValue(bannerId, out PityCounter counter))
            {
                return counter;
            }

            // SaveSystem에서 로드 시도
            counter = LoadPityCounter(bannerId);
            if (counter == null)
            {
                counter = new PityCounter(bannerId);
            }

            _pityCounters[bannerId] = counter;
            return counter;
        }

        private GachaReward[] ApplyMultiGuarantee(GachaReward[] rewards, GachaBannerData banner)
        {
            ItemGrade minGrade = banner.MultiGuaranteedMinGrade;
            bool hasMinGrade = false;
            int lowestIndex = rewards.Length - 1;
            ItemGrade lowestGrade = ItemGrade.Legendary;

            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].Grade >= minGrade)
                {
                    hasMinGrade = true;
                    break;
                }

                if (rewards[i].Grade < lowestGrade)
                {
                    lowestGrade = rewards[i].Grade;
                    lowestIndex = i;
                }
            }

            if (!hasMinGrade && banner.DropTable != null)
            {
                // 최하위 등급 슬롯을 최소 보장 등급으로 승격
                DropEntry upgraded = FindEntryByMinGrade(banner.DropTable, minGrade);

                if (upgraded != null)
                {
                    rewards[lowestIndex] = new GachaReward
                    {
                        RewardId = upgraded.ItemId,
                        RewardType = upgraded.ItemType,
                        Grade = upgraded.Grade,
                        Amount = 1,
                        IsNew = false,
                        IsDuplicate = false
                    };

                    Debug.Log($"[GachaSystem] Multi guarantee applied: upgraded to {upgraded.Grade}");
                }
            }

            return rewards;
        }

        private DropEntry FindEntryByMinGrade(DropTable dropTable, ItemGrade minGrade)
        {
            var candidates = new List<DropEntry>();

            foreach (DropEntry entry in dropTable.Entries)
            {
                if (entry.Grade >= minGrade)
                {
                    candidates.Add(entry);
                }
            }

            if (candidates.Count == 0) return null;

            // 후보 중 가중치 랜덤
            int totalWeight = 0;
            foreach (DropEntry entry in candidates)
            {
                totalWeight += entry.Weight;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int accumulated = 0;

            foreach (DropEntry entry in candidates)
            {
                accumulated += entry.Weight;
                if (roll < accumulated)
                {
                    return entry;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private GachaReward[] ProcessDuplicates(GachaReward[] rewards, GachaBannerData banner)
        {
            if (banner.DuplicatePolicy == DuplicatePolicy.Allow) return rewards;
            if (_duplicateHandler == null) return rewards;

            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].IsDuplicate)
                {
                    rewards[i] = _duplicateHandler.HandleDuplicate(rewards[i]);

                    _eventBus?.Publish(new GachaDuplicateEvent
                    {
                        BannerId = banner.BannerId,
                        RewardId = rewards[i].RewardId,
                        Grade = rewards[i].Grade,
                        Policy = banner.DuplicatePolicy
                    });
                }
            }

            return rewards;
        }

        private void SavePityCounter(PityCounter counter)
        {
            if (_saveSystem == null) return;

            try
            {
                string key = PITY_SAVE_PREFIX + counter.BannerId;
                _saveSystem.Save(SAVE_SLOT, key, counter);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GachaSystem] Failed to save pity counter: {e.Message}");
            }
        }

        private PityCounter LoadPityCounter(string bannerId)
        {
            if (_saveSystem == null) return null;

            try
            {
                string key = PITY_SAVE_PREFIX + bannerId;
                if (!_saveSystem.HasKey(SAVE_SLOT, key)) return null;
                return _saveSystem.Load<PityCounter>(SAVE_SLOT, key);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GachaSystem] Failed to load pity counter: {e.Message}");
                return null;
            }
        }

        private bool IsBannerActive(GachaBannerData banner)
        {
            if (string.IsNullOrEmpty(banner.StartDate) && string.IsNullOrEmpty(banner.EndDate))
            {
                return true;
            }

            DateTime now = DateTime.Now;

            if (!string.IsNullOrEmpty(banner.StartDate) && DateTime.TryParse(banner.StartDate, out DateTime start))
            {
                if (now < start) return false;
            }

            if (!string.IsNullOrEmpty(banner.EndDate) && DateTime.TryParse(banner.EndDate, out DateTime end))
            {
                if (now > end) return false;
            }

            return true;
        }

        private void AssignDefaultStrategy(GachaBannerData banner)
        {
            switch (banner.PityType)
            {
                case PityType.SoftPity:
                    _strategies[banner.BannerId] = new SoftPityPullStrategy(
                        banner.SoftPityStartCount,
                        banner.HardPityCount,
                        banner.SoftPityRateIncrease
                    );
                    Debug.Log($"[GachaSystem] Auto-assigned SoftPityPullStrategy for: {banner.BannerId}");
                    break;

                case PityType.HardPity:
                case PityType.PickupGuarantee:
                    _strategies[banner.BannerId] = new SoftPityPullStrategy(
                        banner.HardPityCount,
                        banner.HardPityCount,
                        0f
                    );
                    Debug.Log($"[GachaSystem] Auto-assigned HardPity strategy for: {banner.BannerId}");
                    break;

                case PityType.None:
                default:
                    // _defaultStrategy (WeightedPullStrategy) 사용
                    break;
            }
        }
    }
}
