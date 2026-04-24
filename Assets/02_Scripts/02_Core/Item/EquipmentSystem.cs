using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class EquipmentSystem : IEquipmentSystem
    {
        private readonly IInventorySystem _inventory;
        private readonly IItemRepository _repo;
        private readonly IEventBus _bus;
        private readonly Dictionary<string, Dictionary<string, string>> _equipped = new();

        public EquipmentSystem(IInventorySystem inventory, IItemRepository repo, IEventBus bus)
        {
            _inventory = inventory;
            _repo = repo;
            _bus = bus;
            Debug.Log("[Equipment] Init");
        }

        public bool Equip(string ownerId, string slotId, string itemInstanceId)
        {
            var inst = _inventory.GetInstance(itemInstanceId);
            if (inst == null) return false;
            if (!_repo.TryGetItem(inst.MID, out var item)) return false;
            if (item.Category != ItemCategory.Equipment) return false;
            if (item.SubtypeRef is not IEquipmentInfo) return false;

            if (!_equipped.TryGetValue(ownerId, out var slots))
            {
                slots = new Dictionary<string, string>();
                _equipped[ownerId] = slots;
            }

            slots.TryGetValue(slotId, out var oldId);
            slots[slotId] = itemInstanceId;
            _bus.Publish(new EquipChangedEvent(ownerId, slotId, oldId, itemInstanceId));
            Debug.Log($"[Equipment] Equip owner={ownerId} slot={slotId} id={itemInstanceId}");
            return true;
        }

        public bool Unequip(string ownerId, string slotId)
        {
            if (!_equipped.TryGetValue(ownerId, out var slots)) return false;
            if (!slots.TryGetValue(slotId, out var oldId)) return false;
            slots.Remove(slotId);
            _bus.Publish(new EquipChangedEvent(ownerId, slotId, oldId, null));
            Debug.Log($"[Equipment] Unequip owner={ownerId} slot={slotId}");
            return true;
        }

        public IItemInstance GetEquipped(string ownerId, string slotId)
        {
            if (!_equipped.TryGetValue(ownerId, out var slots)) return null;
            if (!slots.TryGetValue(slotId, out var id)) return null;
            return _inventory.GetInstance(id);
        }

        public IReadOnlyDictionary<string, IItemInstance> GetAllEquipped(string ownerId)
        {
            var result = new Dictionary<string, IItemInstance>();
            if (!_equipped.TryGetValue(ownerId, out var slots)) return result;
            foreach (var kv in slots)
            {
                var inst = _inventory.GetInstance(kv.Value);
                if (inst != null) result[kv.Key] = inst;
            }
            return result;
        }

        public IReadOnlyDictionary<int, int> GetSetPieceCounts(string ownerId)
        {
            var result = new Dictionary<int, int>();
            if (!_equipped.TryGetValue(ownerId, out var slots)) return result;

            foreach (var instId in slots.Values)
            {
                var inst = _inventory.GetInstance(instId);
                if (inst == null) continue;
                if (!_repo.TryGetItem(inst.MID, out var item)) continue;
                if (item.SubtypeRef is not IEquipmentInfo info) continue;
                if (info.SetId == 0) continue;
                result[info.SetId] = result.TryGetValue(info.SetId, out var c) ? c + 1 : 1;
            }
            return result;
        }
    }
}

