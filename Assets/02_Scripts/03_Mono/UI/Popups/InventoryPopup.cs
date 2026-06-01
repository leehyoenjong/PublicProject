using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 인벤토리 팝업. 보유 아이템 전체를 그리드 슬롯으로 표시한다.
    /// IInventorySystem.GetAll() 로 인스턴스를 가져오고, IItemRepository 로 표시정보(아이콘/카테고리)를 조회한다.
    /// 아이템 변동(Acquired/Consumed/Converted/Expired) 시 자동 갱신.
    /// </summary>
    public class InventoryPopup : BasePopup
    {
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private Button _closeButton;

        private IInventorySystem _inventory;
        private IItemRepository _repo;
        private IEventBus _eventBus;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private Action<ItemAcquiredEvent> _onAcquired;
        private Action<ItemConsumedEvent> _onConsumed;
        private Action<ItemConvertedEvent> _onConverted;
        private Action<ItemExpiredEvent> _onExpired;

        public override void Show(object data)
        {
            base.Show(data);
            ResolveServices();
            Refresh();
        }

        private void OnEnable()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(OnClose);

            ResolveServices();
            if (_eventBus != null)
            {
                _onAcquired = _ => Refresh();
                _onConsumed = _ => Refresh();
                _onConverted = _ => Refresh();
                _onExpired = _ => Refresh();
                _eventBus.Subscribe(_onAcquired);
                _eventBus.Subscribe(_onConsumed);
                _eventBus.Subscribe(_onConverted);
                _eventBus.Subscribe(_onExpired);
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnClose);

            if (_eventBus != null)
            {
                if (_onAcquired != null) _eventBus.Unsubscribe(_onAcquired);
                if (_onConsumed != null) _eventBus.Unsubscribe(_onConsumed);
                if (_onConverted != null) _eventBus.Unsubscribe(_onConverted);
                if (_onExpired != null) _eventBus.Unsubscribe(_onExpired);
            }
        }

        private void ResolveServices()
        {
            if (_inventory == null && ServiceLocator.Has<IInventorySystem>())
                _inventory = ServiceLocator.Get<IInventorySystem>();
            if (_repo == null && ServiceLocator.Has<IItemRepository>())
                _repo = ServiceLocator.Get<IItemRepository>();
            if (_eventBus == null && ServiceLocator.Has<IEventBus>())
                _eventBus = ServiceLocator.Get<IEventBus>();
        }

        private void OnClose()
        {
            SetResult(PopupResult.Close);
        }

        private void Refresh()
        {
            if (_gridContainer == null || _slotPrefab == null) return;

            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();

            if (_inventory == null)
            {
                Debug.LogWarning("[인벤토리팝업] IInventorySystem 미등록 — 표시 생략");
                return;
            }

            IReadOnlyList<IItemInstance> items = _inventory.GetAll();
            foreach (IItemInstance inst in items)
            {
                GameObject go = Instantiate(_slotPrefab, _gridContainer);
                _spawned.Add(go);

                InventorySlotView view = go.GetComponent<InventorySlotView>();
                if (view != null)
                {
                    IItem item = _repo != null ? _repo.GetItem(inst.MID) : null;
                    view.Bind(inst.MID, inst.Count, item);
                }
            }

            Debug.Log($"[인벤토리팝업] 슬롯 {items.Count}개 표시");
        }
    }
}
