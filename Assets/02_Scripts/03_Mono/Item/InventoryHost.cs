using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 인벤토리 UI 와 IInventorySystem 사이의 진입점. Add/Consume/Get 메서드 노출 + 변동 이벤트 4종 구독.
    /// 변동 시 InventoryChanged C# event 1개로 통합 발화 — UI 가 한 곳 구독으로 갱신.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryHost : MonoBehaviour
    {
        private IInventorySystem _inventory;
        private IEventBus _eventBus;

        private Action<ItemAcquiredEvent> _onAcquired;
        private Action<ItemConsumedEvent> _onConsumed;
        private Action<ItemConvertedEvent> _onConverted;
        private Action<ItemExpiredEvent> _onExpired;

        public event Action InventoryChanged;

        public bool IsReady => _inventory != null;

        private void Awake()
        {
            _inventory = ServiceLocator.Has<IInventorySystem>() ? ServiceLocator.Get<IInventorySystem>() : null;
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;

            if (_inventory == null)
            {
                Debug.LogWarning("[InventoryHost] IInventorySystem 미등록 — 기능 비활성", this);
            }
        }

        private void OnEnable()
        {
            if (_eventBus == null) return;
            _onAcquired = _ => InventoryChanged?.Invoke();
            _onConsumed = _ => InventoryChanged?.Invoke();
            _onConverted = _ => InventoryChanged?.Invoke();
            _onExpired = _ => InventoryChanged?.Invoke();
            _eventBus.Subscribe(_onAcquired);
            _eventBus.Subscribe(_onConsumed);
            _eventBus.Subscribe(_onConverted);
            _eventBus.Subscribe(_onExpired);
        }

        private void OnDisable()
        {
            if (_eventBus == null) return;
            if (_onAcquired != null) _eventBus.Unsubscribe(_onAcquired);
            if (_onConsumed != null) _eventBus.Unsubscribe(_onConsumed);
            if (_onConverted != null) _eventBus.Unsubscribe(_onConverted);
            if (_onExpired != null) _eventBus.Unsubscribe(_onExpired);
        }

        public ItemAddResult Add(int mid, int count, object source = null)
        {
            if (_inventory == null) return default;
            return _inventory.AddItem(mid, count, source);
        }

        public bool Consume(int mid, int count)
        {
            if (_inventory == null) return false;
            return _inventory.ConsumeByMID(mid, count);
        }

        public bool ConsumeInstance(string instanceId, int count)
        {
            if (_inventory == null) return false;
            return _inventory.ConsumeByInstance(instanceId, count);
        }

        public int GetCount(int mid)
        {
            return _inventory != null ? _inventory.GetCount(mid) : 0;
        }

        public IItemInstance Get(string instanceId)
        {
            return _inventory?.GetInstance(instanceId);
        }

        public IReadOnlyList<IItemInstance> GetAll()
        {
            return _inventory != null ? _inventory.GetAll() : Array.Empty<IItemInstance>();
        }

        public IReadOnlyList<IItemInstance> GetByCategory(ItemCategory category)
        {
            return _inventory != null ? _inventory.GetByCategory(category) : Array.Empty<IItemInstance>();
        }
    }
}
