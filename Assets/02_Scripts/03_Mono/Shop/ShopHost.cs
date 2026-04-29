using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 상점 UI 와 IShopSystem 사이의 진입점 (MonoBehaviour). UI 버튼이 본 컴포넌트를 호출하고,
    /// 결과는 C# event 로 노출해 UI 가 구독.
    /// IShopContext 는 내부 ShopRuntimeContext 인스턴스로 자동 생성. PlayerLevel/Quest resolver 는 외부에서 주입.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopHost : MonoBehaviour
    {
        [Header("초기 컨텍스트")]
        [SerializeField] private int _initialPlayerLevel = 1;

        private IShopSystem _shopSystem;
        private ShopRuntimeContext _context;

        public event Action<PurchaseResult> PurchaseResulted;

        public IShopContext Context => _context;

        private void Awake()
        {
            _shopSystem = ServiceLocator.Has<IShopSystem>() ? ServiceLocator.Get<IShopSystem>() : null;
            _context = new ShopRuntimeContext(_initialPlayerLevel);

            if (_shopSystem == null)
            {
                Debug.LogWarning("[ShopHost] IShopSystem 미등록 — Refresh/Purchase 비활성", this);
            }
        }

        public IReadOnlyList<IShopProduct> Refresh()
        {
            if (_shopSystem == null) return Array.Empty<IShopProduct>();
            return _shopSystem.GetVisibleProducts(_context);
        }

        public PurchaseEligibility CheckPurchase(string productMID)
        {
            if (_shopSystem == null)
            {
                return new PurchaseEligibility { CanBuy = false, BlockReason = "ShopSystem unavailable" };
            }
            return _shopSystem.CanPurchase(productMID, _context);
        }

        public void Purchase(string productMID, Action<PurchaseResult> onResult = null)
        {
            if (_shopSystem == null)
            {
                var fail = new PurchaseResult
                {
                    Success = false,
                    ProductMID = productMID,
                    FailureReason = "ShopSystem unavailable",
                };
                onResult?.Invoke(fail);
                PurchaseResulted?.Invoke(fail);
                return;
            }

            _shopSystem.Purchase(productMID, _context, result =>
            {
                Debug.Log($"[ShopHost] Purchase result: {result.ProductMID} success={result.Success}");
                onResult?.Invoke(result);
                PurchaseResulted?.Invoke(result);
            });
        }

        public void SetPlayerLevel(int level) => _context.SetPlayerLevel(level);
        public void SetQuestResolver(Func<int, bool> resolver) => _context.SetQuestResolver(resolver);
    }
}
