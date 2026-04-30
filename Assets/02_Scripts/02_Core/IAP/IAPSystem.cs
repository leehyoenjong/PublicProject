using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IIAPSystem 구현체.
    /// 구매 흐름 5단계: 요청 → 스토어 구매 → 영수증 검증 → 보상 지급 → 확인.
    /// </summary>
    public class IAPSystem : IIAPSystem
    {
        private readonly IStoreAdapter _storeAdapter;
        private readonly IReceiptValidator _receiptValidator;
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly IAPConfig _config;

        private readonly List<IAPProductData> _products = new List<IAPProductData>();
        private readonly List<PurchaseReceipt> _purchaseHistory = new List<PurchaseReceipt>();
        private readonly Dictionary<string, int> _purchaseCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, SubscriptionState> _subscriptionStates = new Dictionary<string, SubscriptionState>();

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_HISTORY = "iap_purchase_history";
        private const string SAVE_KEY_COUNTS = "iap_purchase_counts";

        private bool _isPurchasing;

        public IAPSystem(IStoreAdapter storeAdapter, IReceiptValidator receiptValidator,
            IEventBus eventBus, ISaveSystem saveSystem, IAPConfig config)
        {
            _storeAdapter = storeAdapter;
            _receiptValidator = receiptValidator;
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _config = config;

            LoadPurchaseData();
            LoadProducts();

            Debug.Log("[결제] 초기화 시작.");
        }

        public void Purchase(string productId, Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail)
        {
            if (_isPurchasing)
            {
                Debug.LogWarning("[결제] 구매가 이미 진행 중임.");
                onFail?.Invoke(PurchaseFailReason.StoreError);
                return;
            }

            IAPProductData product = GetProduct(productId);
            if (product == null)
            {
                Debug.LogError($"[결제] 상품을 찾을 수 없음: {productId}");
                onFail?.Invoke(PurchaseFailReason.ProductNotFound);
                return;
            }

            if (product.PurchaseLimit > 0 && GetPurchaseCount(productId) >= product.PurchaseLimit)
            {
                Debug.LogWarning($"[결제] 구매 한도 도달: {productId}");
                onFail?.Invoke(PurchaseFailReason.PurchaseLimitReached);
                return;
            }

            _isPurchasing = true;

            _eventBus?.Publish(new PurchaseRequestEvent
            {
                ProductId = productId,
                ProductType = product.ProductType
            });

            Debug.Log($"[결제] 구매 시작: {productId}");

            _storeAdapter.Purchase(productId,
                receipt => OnPurchaseSuccess(receipt, product, onSuccess, onFail),
                reason =>
                {
                    _isPurchasing = false;
                    _eventBus?.Publish(new PurchaseFailEvent
                    {
                        ProductId = productId,
                        Reason = reason
                    });
                    Debug.Log($"[결제] 구매 실패: {productId} ({reason})");
                    onFail?.Invoke(reason);
                });
        }

        public IReadOnlyList<IAPProductData> GetProducts()
        {
            return _products.AsReadOnly();
        }

        public IAPProductData GetProduct(string productId)
        {
            foreach (IAPProductData product in _products)
            {
                if (product.ProductId == productId) return product;
            }
            return null;
        }

        public IReadOnlyList<PurchaseReceipt> GetPurchaseHistory(string productId = null)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return _purchaseHistory.AsReadOnly();
            }

            var filtered = new List<PurchaseReceipt>();
            foreach (PurchaseReceipt receipt in _purchaseHistory)
            {
                if (receipt.ProductId == productId) filtered.Add(receipt);
            }
            return filtered.AsReadOnly();
        }

        public void RestorePurchases(Action<int> onComplete, Action<PurchaseFailReason> onFail)
        {
            Debug.Log("[결제] 구매 복원 시작.");

            _storeAdapter.RestorePurchases(
                receipts =>
                {
                    int total = receipts.Count;
                    int processed = 0;
                    int restored = 0;

                    if (total == 0)
                    {
                        Debug.Log("[결제] 복원할 구매 내역 없음.");
                        onComplete?.Invoke(0);
                        return;
                    }

                    foreach (PurchaseReceipt receipt in receipts)
                    {
                        ValidateAndProcess(receipt,
                            validReceipt =>
                            {
                                IAPProductData product = GetProduct(validReceipt.ProductId);
                                if (product != null)
                                {
                                    GrantRewards(product, "Restore");
                                    RecordPurchase(validReceipt, product);
                                }
                                _storeAdapter.ConfirmPurchase(validReceipt.TransactionId);
                                restored++;
                                processed++;

                                if (processed >= total)
                                {
                                    Debug.Log($"[결제] 복원 완료: {restored}/{total}");
                                    onComplete?.Invoke(restored);
                                }
                            },
                            reason =>
                            {
                                Debug.LogWarning($"[결제] 복원 검증 실패: {receipt.ProductId} ({reason})");
                                processed++;

                                if (processed >= total)
                                {
                                    Debug.Log($"[결제] 복원 완료: {restored}/{total}");
                                    onComplete?.Invoke(restored);
                                }
                            });
                    }
                },
                reason =>
                {
                    Debug.LogError($"[결제] 복원 실패: {reason}");
                    onFail?.Invoke(reason);
                });
        }

        public SubscriptionState GetSubscriptionState(string productId)
        {
            return _subscriptionStates.TryGetValue(productId, out SubscriptionState state)
                ? state : SubscriptionState.None;
        }

        public void ProcessPendingPurchases()
        {
            List<PurchaseReceipt> pending = _storeAdapter.GetPendingPurchases();

            if (pending == null || pending.Count == 0) return;

            int total = pending.Count;
            int processed = 0;
            int resolved = 0;

            Debug.Log($"[결제] 미처리 구매 {total}건 처리 중.");

            foreach (PurchaseReceipt receipt in pending)
            {
                ValidateAndProcess(receipt,
                    validReceipt =>
                    {
                        IAPProductData product = GetProduct(validReceipt.ProductId);
                        if (product != null)
                        {
                            GrantRewards(product, "Pending");
                            RecordPurchase(validReceipt, product);
                        }
                        _storeAdapter.ConfirmPurchase(validReceipt.TransactionId);
                        resolved++;
                        processed++;

                        if (processed >= total)
                        {
                            Debug.Log($"[결제] 미처리 완료: {resolved}/{total} 처리됨.");
                        }
                    },
                    reason =>
                    {
                        Debug.LogWarning($"[결제] 미처리 검증 실패: {receipt.ProductId} ({reason})");
                        processed++;

                        if (processed >= total)
                        {
                            Debug.Log($"[결제] 미처리 완료: {resolved}/{total} 처리됨.");
                        }
                    });
            }
        }

        private void OnPurchaseSuccess(PurchaseReceipt receipt, IAPProductData product,
            Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail)
        {
            ValidateAndProcess(receipt,
                validReceipt =>
                {
                    _isPurchasing = false;
                    GrantRewards(product, "IAP");

                    if (product.ProductType == IAPProductType.Subscription
                        && product.SubscriptionData?.ImmediateRewards != null)
                    {
                        GrantSubscriptionImmediateRewards(product);
                    }

                    RecordPurchase(validReceipt, product);

                    _eventBus?.Publish(new PurchaseCompleteEvent
                    {
                        ProductId = product.ProductId,
                        TransactionId = validReceipt.TransactionId,
                        ProductType = product.ProductType
                    });

                    _storeAdapter.ConfirmPurchase(validReceipt.TransactionId);

                    Debug.Log($"[결제] 구매 완료: {product.ProductId}");
                    onSuccess?.Invoke(validReceipt);
                },
                reason =>
                {
                    _isPurchasing = false;
                    onFail?.Invoke(reason);
                });
        }

        private void ValidateAndProcess(PurchaseReceipt receipt,
            Action<PurchaseReceipt> onValid, Action<PurchaseFailReason> onInvalid)
        {
            _receiptValidator.Validate(receipt, result =>
            {
                _eventBus?.Publish(new PendingResolvedEvent
                {
                    ProductId = receipt.ProductId,
                    TransactionId = receipt.TransactionId,
                    IsValid = result.IsValid
                });

                if (result.IsValid)
                {
                    Debug.Log($"[결제] 영수증 유효: {receipt.TransactionId}");
                    onValid?.Invoke(receipt);
                }
                else
                {
                    Debug.LogError($"[결제] 영수증 무효: {result.Error}");
                    onInvalid?.Invoke(PurchaseFailReason.ValidationFailed);
                }
            });
        }

        private void GrantRewards(IAPProductData product, string source)
        {
            if (product.Rewards == null) return;

            foreach (IAPRewardEntry reward in product.Rewards)
            {
                _eventBus?.Publish(new RewardGrantedEvent
                {
                    ProductId = product.ProductId,
                    RewardId = reward.RewardId,
                    RewardType = reward.RewardType,
                    Amount = reward.Amount,
                    Source = source
                });

                Debug.Log($"[결제] 보상 지급: {reward.RewardId} x{reward.Amount}");
            }
        }

        private void GrantSubscriptionImmediateRewards(IAPProductData product)
        {
            foreach (IAPRewardEntry reward in product.SubscriptionData.ImmediateRewards)
            {
                _eventBus?.Publish(new RewardGrantedEvent
                {
                    ProductId = product.ProductId,
                    RewardId = reward.RewardId,
                    RewardType = reward.RewardType,
                    Amount = reward.Amount,
                    Source = "Subscription_Immediate"
                });

                Debug.Log($"[결제] 구독 즉시 보상: {reward.RewardId} x{reward.Amount}");
            }
        }

        private void RecordPurchase(PurchaseReceipt receipt, IAPProductData product)
        {
            _purchaseHistory.Add(receipt);

            if (!_purchaseCounts.ContainsKey(receipt.ProductId))
            {
                _purchaseCounts[receipt.ProductId] = 0;
            }
            _purchaseCounts[receipt.ProductId]++;

            if (product.ProductType == IAPProductType.Subscription)
            {
                SubscriptionState oldState = GetSubscriptionState(product.ProductId);
                _subscriptionStates[product.ProductId] = SubscriptionState.Active;

                _eventBus?.Publish(new SubscriptionStateChangedEvent
                {
                    ProductId = product.ProductId,
                    OldState = oldState,
                    NewState = SubscriptionState.Active
                });
            }

            SavePurchaseData();
        }

        private int GetPurchaseCount(string productId)
        {
            return _purchaseCounts.TryGetValue(productId, out int count) ? count : 0;
        }

        private void LoadProducts()
        {
            if (_config == null) return;

            IAPProductData[] configProducts = _config.GetProducts();
            if (configProducts != null)
            {
                _products.AddRange(configProducts);
            }

            Debug.Log($"[결제] {_products.Count}개 상품 로드됨.");
        }

        private void LoadPurchaseData()
        {
            if (_saveSystem == null) return;

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_HISTORY))
            {
                var history = _saveSystem.Load<List<PurchaseReceipt>>(SAVE_SLOT, SAVE_KEY_HISTORY);
                if (history != null) _purchaseHistory.AddRange(history);
            }

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_COUNTS))
            {
                var counts = _saveSystem.Load<Dictionary<string, int>>(SAVE_SLOT, SAVE_KEY_COUNTS);
                if (counts != null)
                {
                    foreach (var kvp in counts)
                    {
                        _purchaseCounts[kvp.Key] = kvp.Value;
                    }
                }
            }

            Debug.Log($"[결제] {_purchaseHistory.Count}개 구매 기록 로드됨.");
        }

        private void SavePurchaseData()
        {
            if (_saveSystem == null) return;

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_HISTORY, _purchaseHistory);
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_COUNTS, _purchaseCounts);
            _saveSystem.WriteToDisk(SAVE_SLOT);
        }
    }
}
