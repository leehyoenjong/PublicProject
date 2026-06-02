using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 상점(재화 교환소) 팝업. IShopSystem.GetVisibleProducts 로 노출 상품을 슬롯으로 표시하고,
    /// 슬롯 구매 버튼 → IShopSystem.Purchase(재화 차감 → 보상 인벤토리 적립). 구매 완료/실패/재고 변경 시 자동 갱신.
    /// 결제는 Item(재화) 경로만 시연 — 현금 결제(IAP)는 IIAPSystem(영수증 검증 seam, 미부팅)으로 분리.
    /// InventoryPopup 과 동일하게 host 없이 IShopSystem 직접 조회 패턴.
    ///
    /// 갱신은 즉시 재구성하지 않고 _dirty 플래그를 세워 LateUpdate 에서 한 번만 재구성한다.
    /// 구매 버튼 onClick → Purchase 는 동기 경로라 이벤트가 콜백 스택 안에서 발행되는데,
    /// 그 시점에 슬롯을 Destroy/재생성하면 클릭 처리 중인 버튼이 파괴되는 재진입이 된다.
    /// 또 한 번의 구매가 완료+재고 두 이벤트를 발행하므로, 프레임당 1회 재구성으로 합쳐 중복 teardown 도 막는다.
    /// </summary>
    public class ShopPopup : BasePopup
    {
        [SerializeField] private Transform _listContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private Button _closeButton;
        // 데모 고정값 — 실게임은 캐릭터/스탯 시스템에서 주입(조건부 상품 노출용). 의도적 빈칸.
        [SerializeField] private int _playerLevel = 1;

        private IShopSystem _shop;
        private IEventBus _eventBus;
        private ShopRuntimeContext _context;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private bool _dirty;

        private Action<ShopPurchaseCompletedEvent> _onCompleted;
        private Action<ShopPurchaseFailedEvent> _onFailed;
        private Action<ShopStockChangedEvent> _onStockChanged;

        public override void Show(object data)
        {
            base.Show(data);
            ResolveServices();
            _dirty = true;
        }

        private void OnEnable()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(OnClose);

            ResolveServices();
            if (_eventBus != null)
            {
                _onCompleted = _ => _dirty = true;
                _onFailed = _ => _dirty = true;
                _onStockChanged = _ => _dirty = true;
                _eventBus.Subscribe(_onCompleted);
                _eventBus.Subscribe(_onFailed);
                _eventBus.Subscribe(_onStockChanged);
            }
            _dirty = true;
        }

        private void OnDisable()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnClose);

            if (_eventBus != null)
            {
                if (_onCompleted != null) _eventBus.Unsubscribe(_onCompleted);
                if (_onFailed != null) _eventBus.Unsubscribe(_onFailed);
                if (_onStockChanged != null) _eventBus.Unsubscribe(_onStockChanged);
            }
        }

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            Rebuild();
        }

        private void ResolveServices()
        {
            if (_shop == null && ServiceLocator.Has<IShopSystem>())
                _shop = ServiceLocator.Get<IShopSystem>();
            if (_eventBus == null && ServiceLocator.Has<IEventBus>())
                _eventBus = ServiceLocator.Get<IEventBus>();
            if (_context == null)
                _context = new ShopRuntimeContext(_playerLevel);
        }

        private void OnClose()
        {
            SetResult(PopupResult.Close);
        }

        private void Rebuild()
        {
            if (_listContainer == null || _slotPrefab == null) return;

            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();

            if (_shop == null)
            {
                Debug.LogWarning("[상점팝업] IShopSystem 미등록 — 표시 생략");
                return;
            }

            IReadOnlyList<IShopProduct> products = _shop.GetVisibleProducts(_context);
            foreach (IShopProduct product in products)
            {
                if (product == null) continue;
                GameObject go = Instantiate(_slotPrefab, _listContainer);
                _spawned.Add(go);

                ShopSlotView view = go.GetComponent<ShopSlotView>();
                if (view != null)
                {
                    PurchaseEligibility e = _shop.CanPurchase(product.MID, _context);
                    view.Bind(product, e.CanBuy, OnBuyRequested);
                }
            }

            Debug.Log($"[상점팝업] 상품 {products.Count}개 표시");
        }

        private void OnBuyRequested(string productMid)
        {
            if (_shop == null || string.IsNullOrEmpty(productMid)) return;

            // 결과 갱신은 ShopSystem 이 발행하는 완료/실패/재고 이벤트가 _dirty 를 세워 LateUpdate 에서 일괄 처리. 콜백은 로그만.
            _shop.Purchase(productMid, _context, result =>
            {
                if (result.Success)
                    Debug.Log($"[상점팝업] 구매 성공: {result.ProductMID}");
                else
                    Debug.LogWarning($"[상점팝업] 구매 실패: {result.ProductMID} ({result.FailureReason})");
            });
        }
    }
}
