using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class InventorySystem : IInventorySystem
    {
        private readonly IItemRepository _repo;
        private readonly IEventBus _bus;
        private readonly Dictionary<string, ItemInstance> _instances = new();
        private readonly Dictionary<int, string> _stackIndex = new();

        public InventorySystem(IItemRepository repo, IEventBus bus)
        {
            _repo = repo;
            _bus = bus;
            Debug.Log("[Inventory] Init");
        }

        public ItemAddResult AddItem(int mid, int count, object source)
        {
            if (count <= 0 || !_repo.TryGetItem(mid, out var item))
            {
                Debug.LogWarning($"[Inventory] AddItem failed: mid={mid}, count={count}");
                return new ItemAddResult(false, mid, count, 0, null, Array.Empty<ConvertedReward>());
            }

            switch (item.StackType)
            {
                case StackType.Stack: return AddStack(item, count, source);
                case StackType.Instance: return AddInstance(item, count, source);
                case StackType.Convert: return AddConvert(item, count, source);
                default: return new ItemAddResult(false, mid, count, 0, null, Array.Empty<ConvertedReward>());
            }
        }

        private ItemAddResult AddStack(IItem item, int count, object source)
        {
            ItemInstance inst;
            if (_stackIndex.TryGetValue(item.MID, out var existingId))
            {
                inst = _instances[existingId];
            }
            else
            {
                inst = new ItemInstance($"stack_{item.MID}", item.MID, 0, DateTime.UtcNow);
                _instances[inst.InstanceId] = inst;
                _stackIndex[item.MID] = inst.InstanceId;
            }

            int room = Mathf.Max(0, item.MaxStack - inst.Count);
            int addable = Mathf.Min(count, room);
            inst.Count += addable;
            _bus.Publish(new ItemAcquiredEvent(item.MID, addable, inst.InstanceId, source));
            Debug.Log($"[Inventory] Stack +{addable} mid={item.MID} total={inst.Count}");
            return new ItemAddResult(true, item.MID, count, addable, inst.InstanceId, Array.Empty<ConvertedReward>());
        }

        private ItemAddResult AddInstance(IItem item, int count, object source)
        {
            string lastId = null;
            for (int i = 0; i < count; i++)
            {
                string id = Guid.NewGuid().ToString("N");
                var inst = new ItemInstance(id, item.MID, 1, DateTime.UtcNow);
                _instances[id] = inst;
                lastId = id;
                _bus.Publish(new ItemAcquiredEvent(item.MID, 1, id, source));
            }
            Debug.Log($"[Inventory] Instance +{count} mid={item.MID}");
            return new ItemAddResult(true, item.MID, count, count, lastId, Array.Empty<ConvertedReward>());
        }

        private ItemAddResult AddConvert(IItem item, int count, object source)
        {
            var converted = new List<ConvertedReward>();
            int added = 0;
            string firstId = null;

            for (int i = 0; i < count; i++)
            {
                if (!_stackIndex.ContainsKey(item.MID))
                {
                    string id = $"convert_{item.MID}";
                    var inst = new ItemInstance(id, item.MID, 1, DateTime.UtcNow);
                    _instances[id] = inst;
                    _stackIndex[item.MID] = id;
                    firstId = id;
                    added++;
                    _bus.Publish(new ItemAcquiredEvent(item.MID, 1, id, source));
                }
                else if (item.ConvertRewardMID > 0 && item.ConvertRewardCount > 0)
                {
                    AddItem(item.ConvertRewardMID, item.ConvertRewardCount, source);
                    converted.Add(new ConvertedReward(item.ConvertRewardMID, item.ConvertRewardCount));
                    _bus.Publish(new ItemConvertedEvent(item.MID, item.ConvertRewardMID, item.ConvertRewardCount));
                }
            }

            Debug.Log($"[Inventory] Convert mid={item.MID} added={added} converted={converted.Count}");
            return new ItemAddResult(true, item.MID, count, added, firstId, converted);
        }

        public bool ConsumeByMID(int mid, int count)
        {
            if (count <= 0 || !_stackIndex.TryGetValue(mid, out var id)) return false;
            var inst = _instances[id];
            if (inst.Count < count) return false;
            inst.Count -= count;
            _bus.Publish(new ItemConsumedEvent(mid, count, id));
            if (inst.Count == 0)
            {
                _instances.Remove(id);
                _stackIndex.Remove(mid);
            }
            Debug.Log($"[Inventory] Consume mid={mid} count={count}");
            return true;
        }

        public bool ConsumeByInstance(string instanceId, int count)
        {
            if (count <= 0 || !_instances.TryGetValue(instanceId, out var inst)) return false;
            if (inst.Count < count) return false;
            inst.Count -= count;
            _bus.Publish(new ItemConsumedEvent(inst.MID, count, instanceId));
            if (inst.Count == 0)
            {
                _instances.Remove(instanceId);
                if (_stackIndex.TryGetValue(inst.MID, out var mapped) && mapped == instanceId)
                    _stackIndex.Remove(inst.MID);
            }
            Debug.Log($"[Inventory] ConsumeInstance id={instanceId} count={count}");
            return true;
        }

        public int GetCount(int mid)
        {
            if (_stackIndex.TryGetValue(mid, out var id)) return _instances[id].Count;
            int sum = 0;
            foreach (var inst in _instances.Values) if (inst.MID == mid) sum += inst.Count;
            return sum;
        }

        public IItemInstance GetInstance(string instanceId)
        {
            _instances.TryGetValue(instanceId, out var inst);
            return inst;
        }

        public IReadOnlyList<IItemInstance> GetAll()
        {
            var list = new List<IItemInstance>(_instances.Count);
            foreach (var inst in _instances.Values) list.Add(inst);
            return list;
        }

        public IReadOnlyList<IItemInstance> GetByCategory(ItemCategory category)
        {
            var list = new List<IItemInstance>();
            foreach (var inst in _instances.Values)
            {
                if (_repo.TryGetItem(inst.MID, out var item) && item.Category == category)
                    list.Add(inst);
            }
            return list;
        }

        public bool SetBound(string instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out var inst)) return false;
            inst.IsBound = true;
            return true;
        }

        public int PurgeExpired()
        {
            var now = DateTime.UtcNow;
            var toRemove = new List<string>();
            foreach (var kv in _instances)
                if (kv.Value.IsExpired(now)) toRemove.Add(kv.Key);

            foreach (var id in toRemove)
            {
                var inst = _instances[id];
                _instances.Remove(id);
                if (_stackIndex.TryGetValue(inst.MID, out var mapped) && mapped == id)
                    _stackIndex.Remove(inst.MID);
                _bus.Publish(new ItemExpiredEvent(inst.MID, id));
            }
            if (toRemove.Count > 0) Debug.Log($"[Inventory] Purged {toRemove.Count} expired");
            return toRemove.Count;
        }
    }
}

