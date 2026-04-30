using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IShopSystem 구현. 상품 노출/구매 자격/구매 처리의 단일 진입점.
    /// 지불은 IPaymentProcessor 전략에 위임, 영속화는 IShopRepository 에 위임, 보상 지급은 IInventorySystem 경유.
    /// </summary>
    public class ShopSystem : IShopSystem
    {
        private readonly IEventBus _eventBus;
        private readonly IShopRepository _repository;
        private readonly IInventorySystem _inventory;
        private readonly ITimeProvider _timeProvider;

        private readonly Dictionary<string, ShopData> _products = new Dictionary<string, ShopData>();
        private readonly Dictionary<string, ShopProductInstance> _instances = new Dictionary<string, ShopProductInstance>();
        private readonly Dictionary<PaymentType, IPaymentProcessor> _processors = new Dictionary<PaymentType, IPaymentProcessor>();

        private DateTime _nextDailyResetUtc;
        private DateTime _nextWeeklyResetUtc;

        public ShopSystem(IEventBus eventBus, IShopRepository repository, IInventorySystem inventory, ITimeProvider timeProvider)
        {
            _eventBus = eventBus;
            _repository = repository;
            _inventory = inventory;
            _timeProvider = timeProvider;
            Debug.Log("[상점] 초기화 시작.");
        }

        /// <summary>ShopDataCollection 주입 후 저장된 인스턴스 복원 + 리셋 경계 초기화.</summary>
        public void Initialize(ShopDataCollection collection)
        {
            Initialize(collection != null ? collection.Items : null);
        }

        /// <summary>인터페이스 기반 Initialize — 테스트에서 Fake 주입 가능.</summary>
        public void Initialize(IReadOnlyList<IShopProduct> products)
        {
            _products.Clear();
            if (products != null)
            {
                foreach (IShopProduct product in products)
                {
                    if (!(product is ShopData data) || string.IsNullOrEmpty(data.MID)) continue;
                    _products[data.MID] = data;
                }
            }

            LoadInstances();
            RecomputeResetBoundaries();

            Debug.Log($"[상점] 초기화 완료 — 상품: {_products.Count}, 인스턴스: {_instances.Count}");
        }

        /// <summary>PaymentType 전략 등록. 같은 타입 재등록 시 덮어쓴다.</summary>
        public void RegisterPaymentProcessor(IPaymentProcessor processor)
        {
            if (processor == null) return;
            _processors[processor.SupportedType] = processor;
            Debug.Log($"[상점] 결제 처리기 등록됨: {processor.SupportedType}");
        }

        public IReadOnlyList<IShopProduct> GetVisibleProducts(IShopContext context)
        {
            TryAdvanceScopeReset();

            var list = new List<IShopProduct>();
            DateTime nowUtc = _timeProvider != null ? _timeProvider.NowUtc : DateTime.UtcNow;

            foreach (ShopData data in _products.Values)
            {
                if (!data.IsActive) continue;
                if (!ShopResetScheduler.IsWithinEventPeriod(data, nowUtc)) continue;
                if (!IsConditionSatisfied(data, context)) continue;

                list.Add(data);
            }

            return list.AsReadOnly();
        }

        public IShopProductInstance GetInstance(string productMID)
        {
            _instances.TryGetValue(productMID, out ShopProductInstance instance);
            return instance;
        }

        public PurchaseEligibility CanPurchase(string productMID, IShopContext context)
        {
            if (!_products.TryGetValue(productMID, out ShopData data))
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "product_not_found" };
            }

            if (!data.IsActive)
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "inactive" };
            }

            DateTime nowUtc = _timeProvider != null ? _timeProvider.NowUtc : DateTime.UtcNow;
            if (!ShopResetScheduler.IsWithinEventPeriod(data, nowUtc))
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "out_of_event_period" };
            }

            if (!IsConditionSatisfied(data, context))
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "condition_not_met" };
            }

            ShopProductInstance instance = EnsureInstance(productMID, data.ProductLimit);

            if (data.ProductLimit > 0 && instance.TotalPurchaseCount >= data.ProductLimit)
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "sold_out" };
            }

            if (data.PlayerLimit > 0 && GetScopeCount(instance, data.PlayerLimitScope) >= data.PlayerLimit)
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "player_limit_reached" };
            }

            if (!_processors.ContainsKey(data.PaymentType))
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "payment_processor_missing" };
            }

            return new PurchaseEligibility { CanBuy = true, BlockReason = null };
        }

        public void Purchase(string productMID, IShopContext context, Action<PurchaseResult> callback)
        {
            PurchaseEligibility eligibility = CanPurchase(productMID, context);
            ShopData data = _products.TryGetValue(productMID, out ShopData d) ? d : null;

            if (!eligibility.CanBuy || data == null)
            {
                FailPurchase(productMID, eligibility.BlockReason ?? "unknown", callback);
                return;
            }

            _eventBus?.Publish(new ShopPurchaseRequestedEvent
            {
                ProductMID = productMID,
                PaymentType = data.PaymentType
            });

            IPaymentProcessor processor = _processors[data.PaymentType];
            processor.Process(data, paymentResult =>
            {
                if (!paymentResult.Success)
                {
                    FailPurchase(productMID, paymentResult.Reason ?? "payment_failed", callback);
                    return;
                }

                CompletePurchase(data, paymentResult.ProviderTransactionId, callback);
            });
        }

        private void CompletePurchase(ShopData data, string providerTxId, Action<PurchaseResult> callback)
        {
            ShopProductInstance instance = EnsureInstance(data.MID, data.ProductLimit);
            long nowUnix = _timeProvider != null ? _timeProvider.NowUnixSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            GrantRewards(data);

            instance.RegisterPurchase(nowUnix, data.ProductLimit);
            _repository?.Save(instance);

            var result = new PurchaseResult
            {
                Success = true,
                ProductMID = data.MID,
                FailureReason = null,
                GrantedRewards = data.Rewards
            };

            _eventBus?.Publish(new ShopPurchaseCompletedEvent
            {
                ProductMID = data.MID,
                PaymentType = data.PaymentType,
                ProviderTransactionId = providerTxId,
                GrantedRewards = data.Rewards
            });

            _eventBus?.Publish(new ShopStockChangedEvent
            {
                ProductMID = data.MID,
                TotalPurchaseCount = instance.TotalPurchaseCount,
                CurrentScopePurchaseCount = instance.CurrentScopePurchaseCount,
                IsSoldOut = instance.IsSoldOut
            });

            Debug.Log($"[상점] 구매 완료: {data.MID} (거래ID: {providerTxId})");
            callback?.Invoke(result);
        }

        private void FailPurchase(string productMID, string reason, Action<PurchaseResult> callback)
        {
            _eventBus?.Publish(new ShopPurchaseFailedEvent
            {
                ProductMID = productMID,
                Reason = reason
            });

            Debug.Log($"[상점] 구매 실패: {productMID} (사유: {reason})");
            callback?.Invoke(new PurchaseResult
            {
                Success = false,
                ProductMID = productMID,
                FailureReason = reason,
                GrantedRewards = null
            });
        }

        private void GrantRewards(ShopData data)
        {
            if (_inventory == null || data.Rewards == null) return;

            foreach (ShopReward reward in data.Rewards)
            {
                if (reward == null || reward.RewardAmount <= 0) continue;
                _inventory.AddItem(reward.RewardItemMID, reward.RewardAmount, "Shop");
            }
        }

        private bool IsConditionSatisfied(ShopData data, IShopContext context)
        {
            if (data.ConditionType == ShopConditionType.None) return true;
            if (context == null) return false;

            switch (data.ConditionType)
            {
                case ShopConditionType.MinLevel:
                    return int.TryParse(data.ConditionValue, out int minLevel) && context.PlayerLevel >= minLevel;
                case ShopConditionType.QuestClear:
                    return int.TryParse(data.ConditionValue, out int questMid) && context.IsQuestCleared(questMid);
                default:
                    return true;
            }
        }

        private ShopProductInstance EnsureInstance(string productMID, int productLimit)
        {
            if (!_instances.TryGetValue(productMID, out ShopProductInstance instance))
            {
                instance = new ShopProductInstance(productMID);
                instance.RecalculateSoldOut(productLimit);
                _instances[productMID] = instance;
            }
            return instance;
        }

        private int GetScopeCount(ShopProductInstance instance, LimitScope scope)
        {
            return scope == LimitScope.Lifetime ? instance.TotalPurchaseCount : instance.CurrentScopePurchaseCount;
        }

        private void LoadInstances()
        {
            _instances.Clear();
            if (_repository == null) return;

            IReadOnlyList<IShopProductInstance> saved = _repository.LoadAll();
            if (saved == null) return;

            foreach (IShopProductInstance s in saved)
            {
                if (s == null || string.IsNullOrEmpty(s.ProductMID)) continue;
                int limit = _products.TryGetValue(s.ProductMID, out ShopData data) ? data.ProductLimit : 0;

                _instances[s.ProductMID] = new ShopProductInstance(
                    s.ProductMID,
                    s.TotalPurchaseCount,
                    s.CurrentScopePurchaseCount,
                    s.LastPurchaseAtUtc,
                    limit
                );
            }
        }

        private void RecomputeResetBoundaries()
        {
            DateTime nowUtc = _timeProvider != null ? _timeProvider.NowUtc : DateTime.UtcNow;

            // Daily = 다음 UTC 09:00
            DateTime todayReset = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, ShopResetScheduler.RESET_HOUR_UTC, 0, 0, DateTimeKind.Utc);
            _nextDailyResetUtc = nowUtc < todayReset ? todayReset : todayReset.AddDays(1);

            // Weekly = 다음 월요일 09:00 UTC (기본 경계)
            _nextWeeklyResetUtc = _nextDailyResetUtc;
            while (_nextWeeklyResetUtc.DayOfWeek != DayOfWeek.Monday)
            {
                _nextWeeklyResetUtc = _nextWeeklyResetUtc.AddDays(1);
            }
        }

        private void TryAdvanceScopeReset()
        {
            if (_timeProvider == null) return;
            DateTime nowUtc = _timeProvider.NowUtc;

            if (nowUtc >= _nextDailyResetUtc)
            {
                ResetScope(LimitScope.Day);
                _nextDailyResetUtc = _nextDailyResetUtc.AddDays(1);
            }

            if (nowUtc >= _nextWeeklyResetUtc)
            {
                ResetScope(LimitScope.Week);
                _nextWeeklyResetUtc = _nextWeeklyResetUtc.AddDays(7);
            }
        }

        private void ResetScope(LimitScope scope)
        {
            foreach (ShopProductInstance instance in _instances.Values)
            {
                if (!_products.TryGetValue(instance.ProductMID, out ShopData data)) continue;
                if (data.PlayerLimitScope != scope) continue;

                instance.ResetScopeCount();
                _repository?.Save(instance);
            }

            _repository?.ResetScope(scope);

            _eventBus?.Publish(new ShopResetEvent { Scope = scope });
            Debug.Log($"[상점] 범위 초기화됨: {scope}");
        }
    }
}
