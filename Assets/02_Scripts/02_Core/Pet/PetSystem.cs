using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 펫 도메인 런타임 진입점. PetInfo 룩업, 인스턴스 관리, 다중 슬롯 장착/해제, 스킬 슬롯 변경, 훅 트리거.
    /// AI/BT 결합과 따라다니기 Mono 동작은 후속 Phase 책임 (본 시스템은 데이터·상태·이벤트만 담당).
    /// </summary>
    public class PetSystem : IPetSystem
    {
        private readonly Dictionary<string, IPetInfo> _infoByMID = new();
        private readonly Dictionary<int, IPetInfo> _infoByItemMID = new();
        private readonly Dictionary<string, PetInstance> _instances = new();

        private PetInstance[] _slots;
        private readonly IEventBus _eventBus;

        public PetSystem(int maxSlots = 1, IEventBus eventBus = null)
        {
            int clamped = maxSlots < 1 ? 1 : maxSlots;
            _slots = new PetInstance[clamped];
            _eventBus = eventBus;
            Debug.Log($"[PetSystem] Init started — maxSlots: {clamped}");
        }

        public int MaxSlots => _slots.Length;

        public IReadOnlyList<IPetInstance> EquippedSlots => _slots;

        public void Initialize(PetInfoCollection pets)
        {
            _infoByMID.Clear();
            _infoByItemMID.Clear();
            if (pets?.Items != null)
            {
                foreach (PetInfo info in pets.Items)
                {
                    if (info == null || string.IsNullOrEmpty(info.MID)) continue;
                    _infoByMID[info.MID] = info;
                    if (info.ItemMID > 0)
                    {
                        _infoByItemMID[info.ItemMID] = info;
                    }
                }
            }
            Debug.Log($"[PetSystem] Initialized — pets: {_infoByMID.Count}");
        }

        public void Initialize(IReadOnlyList<IPetInfo> pets)
        {
            _infoByMID.Clear();
            _infoByItemMID.Clear();
            if (pets != null)
            {
                for (int i = 0; i < pets.Count; i++)
                {
                    IPetInfo info = pets[i];
                    if (info == null || string.IsNullOrEmpty(info.MID)) continue;
                    _infoByMID[info.MID] = info;
                    if (info.ItemMID > 0)
                    {
                        _infoByItemMID[info.ItemMID] = info;
                    }
                }
            }
            Debug.Log($"[PetSystem] Initialized — pets: {_infoByMID.Count}");
        }

        public void SetMaxSlots(int maxSlots)
        {
            int clamped = maxSlots < 1 ? 1 : maxSlots;
            if (clamped == _slots.Length) return;

            var newSlots = new PetInstance[clamped];
            int copy = clamped < _slots.Length ? clamped : _slots.Length;
            for (int i = 0; i < copy; i++)
            {
                newSlots[i] = _slots[i];
            }
            for (int i = clamped; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    UnequipInternal(i, _slots[i]);
                }
            }
            _slots = newSlots;
            Debug.Log($"[PetSystem] MaxSlots changed: {clamped}");
        }

        public IPetInfo GetInfo(string petMID)
        {
            if (string.IsNullOrEmpty(petMID)) return null;
            return _infoByMID.TryGetValue(petMID, out IPetInfo info) ? info : null;
        }

        public IPetInfo GetInfoByItemMID(int itemMID)
        {
            if (itemMID <= 0) return null;
            return _infoByItemMID.TryGetValue(itemMID, out IPetInfo info) ? info : null;
        }

        public IPetInstance Get(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _instances.TryGetValue(instanceId, out PetInstance inst) ? inst : null;
        }

        public IReadOnlyCollection<IPetInstance> All => _instances.Values;

        public IPetInstance GetEquipped(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return null;
            return _slots[slotIndex];
        }

        public IPetInstance Acquire(string petMID, string instanceId, IStatContainer stats)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            IPetInfo info = GetInfo(petMID);
            if (info == null) return null;
            if (_instances.ContainsKey(instanceId)) return null;

            var inst = new PetInstance(instanceId, info, stats);
            _instances[instanceId] = inst;

            List<string> triggered = TriggerHooks(info.OnAcquireEvents, PetEventKind.Acquire, info.MID, instanceId);

            _eventBus?.Publish(new PetAcquiredEvent
            {
                PetMID = info.MID,
                InstanceId = instanceId,
                TriggeredHookIds = triggered,
            });

            Debug.Log($"[PetSystem] Acquired: {info.MID} ({instanceId})");
            return inst;
        }

        public bool Release(string instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out PetInstance inst)) return false;

            if (inst.IsEquipped)
            {
                UnequipInternal(inst.EquippedSlotIndex, inst);
            }
            _instances.Remove(instanceId);

            _eventBus?.Publish(new PetReleasedEvent
            {
                PetMID = inst.Info?.MID,
                InstanceId = instanceId,
            });

            Debug.Log($"[PetSystem] Released: {inst.Info?.MID} ({instanceId})");
            return true;
        }

        public bool Equip(string instanceId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return false;
            if (!_instances.TryGetValue(instanceId, out PetInstance inst)) return false;
            if (inst.EquippedSlotIndex == slotIndex) return true;

            if (_slots[slotIndex] != null)
            {
                UnequipInternal(slotIndex, _slots[slotIndex]);
            }
            if (inst.IsEquipped)
            {
                UnequipInternal(inst.EquippedSlotIndex, inst);
            }

            _slots[slotIndex] = inst;
            inst.SetEquippedSlot(slotIndex);

            List<string> triggered = TriggerHooks(inst.Info?.OnEquipEvents, PetEventKind.Equip, inst.Info?.MID, instanceId);

            _eventBus?.Publish(new PetEquippedEvent
            {
                PetMID = inst.Info?.MID,
                InstanceId = instanceId,
                SlotIndex = slotIndex,
                TriggeredHookIds = triggered,
            });

            Debug.Log($"[PetSystem] Equipped: {inst.Info?.MID} → slot {slotIndex}");
            return true;
        }

        public bool Unequip(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return false;
            PetInstance inst = _slots[slotIndex];
            if (inst == null) return false;

            UnequipInternal(slotIndex, inst);
            return true;
        }

        public bool UnequipInstance(string instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out PetInstance inst)) return false;
            if (!inst.IsEquipped) return false;

            UnequipInternal(inst.EquippedSlotIndex, inst);
            return true;
        }

        public bool SetEquippedSkill(string instanceId, int skillSlot, SkillData skill)
        {
            if (!_instances.TryGetValue(instanceId, out PetInstance inst)) return false;
            if (!inst.TrySetSkill(skillSlot, skill)) return false;

            _eventBus?.Publish(new PetSkillChangedEvent
            {
                InstanceId = instanceId,
                SkillSlot = skillSlot,
                SkillMID = skill != null ? skill.SkillId : null,
            });
            return true;
        }

        private void UnequipInternal(int slotIndex, PetInstance inst)
        {
            _slots[slotIndex] = null;
            inst.ClearEquippedSlot();

            List<string> triggered = TriggerHooks(inst.Info?.OnUnequipEvents, PetEventKind.Unequip, inst.Info?.MID, inst.InstanceId);

            _eventBus?.Publish(new PetUnequippedEvent
            {
                PetMID = inst.Info?.MID,
                InstanceId = inst.InstanceId,
                SlotIndex = slotIndex,
                TriggeredHookIds = triggered,
            });

            Debug.Log($"[PetSystem] Unequipped: {inst.Info?.MID} ← slot {slotIndex}");
        }

        private List<string> TriggerHooks(IReadOnlyList<string> eventIds, PetEventKind kind, string petMID, string instanceId)
        {
            var triggered = new List<string>();
            if (eventIds == null) return triggered;

            for (int i = 0; i < eventIds.Count; i++)
            {
                string id = eventIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                triggered.Add(id);
                _eventBus?.Publish(new PetHookTriggeredEvent
                {
                    EventId = id,
                    Kind = kind,
                    PetMID = petMID,
                    InstanceId = instanceId,
                });
            }
            return triggered;
        }
    }
}
