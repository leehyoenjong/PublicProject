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

            Debug.Log("[IAPSystem] Init started");
        }

        public void Purchase(string productId, Action<PurchaseReceipt> onSuccess, Action<PurchaseFailReason> onFail)
        {
            if (_isPurchasing)
            {
                Debug.LogWarning("[IAPSystem] Purchase already in progress");
                onFail?.Invoke(PurchaseFailReason.StoreError);
                return;
            }

            IAPProductData product = GetProduct(productId);
            if (product == null)
            {
                Debug.LogError($"[IAPSystem] Product not found: {productId}");
                onFail?.Invoke(PurchaseFailReason.ProductNotFound);
                return;
            }

            if (product.PurchaseLimit > 0 && GetPurchaseCount(productId) >= product.PurchaseLimit)
            {
                Debug.LogWarning($"[IAPSystem] Purchase limit reached: {productId}");
                onFail?.Invoke(PurchaseFailReason.PurchaseLimitReached);
                return;
            }

            _isPurchasing = true;

            _eventBus?.Publish(new PurchaseRequestEvent
            {
                ProductId = productId,
                ProductType = product.ProductType
            });

            Debug.Log($"[IAPSystem] Purchase started: {productId}");

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
                    Debug.Log($"[IAPSystem] Purchase failed: {productId} ({reason})");
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
            Debug.Log("[IAPSystem] Restore purchases started");

            _storeAdapter.RestorePurchases(
                receipts =>
                {
                    int total = receipts.Count;
                    int processed = 0;
                    int restored = 0;

                    if (total == 0)
                    {
                        Debug.Log("[IAPSystem] No purchases to restore");
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
                                    Debug.Log($"[IAPSystem] Restored {restored}/{total} purchases");
                                    onComplete?.Invoke(restored);
                                }
                            },
                            reason =>
                            {
                                Debug.LogWarning($"[IAPSystem] Restore validation failed: {receipt.ProductId} ({reason})");
                                processed++;

                                if (processed >= total)
                                {
                                    Debug.Log($"[IAPSystem] Restored {restored}/{total} purchases");
                                    onComplete?.Invoke(restored);
                                }
                            });
                    }
                },
                reason =>
                {
                    Debug.LogError($"[IAPSystem] Restore failed: {reason}");
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

            Debug.Log($"[IAPSystem] Processing {total} pending purchases");

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
                            Debug.Log($"[IAPSystem] Pending complete: {resolved}/{total} resolved");
                        }
                    },
                    reason =>
                    {
                        Debug.LogWarning($"[IAPSystem] Pending validation failed: {receipt.ProductId} ({reason})");
                        processed++;

                        if (processed >= total)
                        {
                            Debug.Log($"[IAPSystem] Pending complete: {resolved}/{total} resolved");
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

                    Debug.Log($"[IAPSystem] Purchase complete: {product.ProductId}");
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
                    Debug.Log($"[IAPSystem] Receipt valid: {receipt.TransactionId}");
                    onValid?.Invoke(receipt);
                }
                else
                {
                    Debug.LogError($"[IAPSystem] Receipt invalid: {result.Error}");
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

                Debug.Log($"[IAPSystem] Reward granted: {reward.RewardId} x{reward.Amount}");
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

                Debug.Log($"[IAPSystem] Subscription immediate reward: {reward.RewardId} x{reward.Amount}");
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

            Debug.Log($"[IAPSystem] Loaded {_products.Count} products");
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

            Debug.Log($"[IAPSystem] Loaded {_purchaseHistory.Count} purchase records");
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
