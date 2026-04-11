using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 버프 아이콘 바 UI — EventBus 구독으로 아이콘 추가/제거
    /// </summary>
    public class BuffIconBar : MonoBehaviour
    {
        [SerializeField] private Transform _slotParent;
        [SerializeField] private BuffIconSlot _slotPrefab;

        private readonly Dictionary<string, BuffIconSlot> _slots = new Dictionary<string, BuffIconSlot>();
        private readonly Dictionary<string, int> _instanceCounter = new Dictionary<string, int>();
        private IEventBus _eventBus;
        private IBuffSystem _buffSystem;
        private string _ownerId;

        public void Init(string ownerId)
        {
            _ownerId = ownerId;
            _eventBus = ServiceLocator.Get<IEventBus>();
            _buffSystem = ServiceLocator.Get<IBuffSystem>();

            _eventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Subscribe<BuffRemovedEvent>(OnBuffRemoved);
            _eventBus.Subscribe<BuffExpiredEvent>(OnBuffExpired);
            _eventBus.Subscribe<BuffStackChangedEvent>(OnBuffStackChanged);

            Debug.Log($"[BuffIconBar] Init for: {_ownerId}");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Unsubscribe<BuffRemovedEvent>(OnBuffRemoved);
            _eventBus.Unsubscribe<BuffExpiredEvent>(OnBuffExpired);
            _eventBus.Unsubscribe<BuffStackChangedEvent>(OnBuffStackChanged);
        }

        private void OnBuffApplied(BuffAppliedEvent evt)
        {
            if (evt.TargetId != _ownerId) return;
            if (_slotPrefab == null || _slotParent == null) return;

            string slotKey = GetNextSlotKey(evt.BuffId);

            BuffIconSlot slot = Instantiate(_slotPrefab, _slotParent);
            IReadOnlyList<IBuffInstance> buffs = _buffSystem.GetBuffs(_ownerId);

            // 마지막으로 추가된 해당 buffId 인스턴스를 찾아 연결
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].BuffId == evt.BuffId && buffs[i] is IBuffUIData uiData)
                {
                    slot.SetData(uiData);
                    break;
                }
            }

            _slots[slotKey] = slot;
        }

        private void OnBuffRemoved(BuffRemovedEvent evt)
        {
            if (evt.TargetId != _ownerId) return;
            RemoveSlot(evt.BuffId);
        }

        private void OnBuffExpired(BuffExpiredEvent evt)
        {
            if (evt.TargetId != _ownerId) return;
            RemoveSlot(evt.BuffId);
        }

        private void OnBuffStackChanged(BuffStackChangedEvent evt)
        {
            if (evt.TargetId != _ownerId) return;

            // 첫 번째로 매칭되는 슬롯의 스택 업데이트
            foreach (var kvp in _slots)
            {
                if (kvp.Key.StartsWith(evt.BuffId))
                {
                    kvp.Value.UpdateStack(evt.NewStack);
                    break;
                }
            }
        }

        private void RemoveSlot(string buffId)
        {
            // 해당 buffId로 시작하는 첫 번째 슬롯을 제거
            string keyToRemove = null;

            foreach (string key in _slots.Keys)
            {
                if (key.StartsWith(buffId))
                {
                    keyToRemove = key;
                    break;
                }
            }

            if (keyToRemove != null && _slots.TryGetValue(keyToRemove, out BuffIconSlot slot))
            {
                _slots.Remove(keyToRemove);
                Destroy(slot.gameObject);
            }
        }

        private string GetNextSlotKey(string buffId)
        {
            if (!_instanceCounter.TryGetValue(buffId, out int count))
            {
                count = 0;
            }

            count++;
            _instanceCounter[buffId] = count;

            return $"{buffId}_{count}";
        }
    }
}
