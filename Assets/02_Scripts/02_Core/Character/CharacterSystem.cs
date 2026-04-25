using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 도메인 런타임 진입점. CharacterInfo 룩업 + CharacterInstance 관리 + 보조 API.
    /// 스킬슬롯 전략은 <see cref="ISkillSlotResolver"/> 로 주입 교체 가능.
    /// </summary>
    public class CharacterSystem : ICharacterSystem
    {
        private readonly Dictionary<int, ICharacterInfo> _infoByMID = new();
        private readonly Dictionary<string, CharacterInstance> _instances = new();
        private readonly ISkillSlotResolver _slotResolver;
        private readonly IEventBus _eventBus;

        public CharacterSystem(ISkillSlotResolver slotResolver = null, IEventBus eventBus = null)
        {
            _slotResolver = slotResolver ?? new DefaultSkillSlotResolver();
            _eventBus = eventBus;
        }

        public void Initialize(CharacterInfoCollection collection)
        {
            _infoByMID.Clear();
            if (collection?.Items == null) return;
            foreach (var info in collection.Items)
            {
                if (info == null) continue;
                _infoByMID[info.ItemMID] = info;
            }
        }

        public void Initialize(IReadOnlyList<ICharacterInfo> infos)
        {
            _infoByMID.Clear();
            if (infos == null) return;
            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                if (info == null) continue;
                _infoByMID[info.ItemMID] = info;
            }
        }

        public ICharacterInfo GetInfo(int itemMID)
        {
            return _infoByMID.TryGetValue(itemMID, out var info) ? info : null;
        }

        public ICharacterInstance Get(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _instances.TryGetValue(instanceId, out var inst) ? inst : null;
        }

        public IReadOnlyCollection<ICharacterInstance> All => _instances.Values;

        public ICharacterInstance Create(int itemMID, string instanceId, IStatContainer stats, int level = 1, Rarity rarity = Rarity.Common)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            var info = GetInfo(itemMID);
            if (info == null) return null;
            if (_instances.ContainsKey(instanceId)) return null;

            var inst = new CharacterInstance(instanceId, info, stats);
            inst.SetLevel(level);
            inst.SetRarity(rarity);
            inst.SetEquippedSkills(info.BaseSkills);
            _instances[instanceId] = inst;

            _eventBus?.Publish(new CharacterCreatedEvent { InstanceId = instanceId, ItemMID = itemMID });
            return inst;
        }

        public bool Remove(string instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out var inst)) return false;
            int mid = inst.Info?.ItemMID ?? 0;
            _instances.Remove(instanceId);
            _eventBus?.Publish(new CharacterRemovedEvent { InstanceId = instanceId, ItemMID = mid });
            return true;
        }

        public bool SetLevel(string instanceId, int level)
        {
            if (!_instances.TryGetValue(instanceId, out var inst)) return false;
            int old = inst.Level;
            inst.SetLevel(level);
            if (old != inst.Level)
                _eventBus?.Publish(new CharacterLevelChangedEvent { InstanceId = instanceId, OldLevel = old, NewLevel = inst.Level });
            return true;
        }

        public bool SetAwakening(string instanceId, int awakening)
        {
            if (!_instances.TryGetValue(instanceId, out var inst)) return false;
            int old = inst.Awakening;
            inst.SetAwakening(awakening);
            if (old != inst.Awakening)
                _eventBus?.Publish(new CharacterAwakeningChangedEvent { InstanceId = instanceId, OldAwakening = old, NewAwakening = inst.Awakening });
            return true;
        }

        public bool SetEquippedSkill(string instanceId, int slot, SkillData skill)
        {
            if (!_instances.TryGetValue(instanceId, out var inst)) return false;
            SkillData old = slot >= 0 && slot < inst.EquippedSkills.Count ? inst.EquippedSkills[slot] : null;
            if (!inst.SetEquippedSkill(slot, skill)) return false;
            if (old != skill)
                _eventBus?.Publish(new CharacterSkillEquippedEvent { InstanceId = instanceId, Slot = slot, OldSkill = old, NewSkill = skill });
            return true;
        }

        public int CalculateSlotCount(ICharacterInstance instance)
        {
            if (instance == null) return 0;
            return _slotResolver.Resolve(instance.Info, instance.Level, instance.Awakening, instance.Rarity);
        }

        public int GetDialogueLine(ICharacterInstance instance, DialogueEvent ev)
        {
            var dialogues = instance?.Info?.Dialogues;
            if (dialogues == null) return 0;
            for (int i = 0; i < dialogues.Count; i++)
            {
                var d = dialogues[i];
                if (d != null && d.Event == ev && d.HasLine) return d.LineKey;
            }
            return 0;
        }

        public string GetProfileValue(ICharacterInstance instance, string key)
        {
            var entry = GetProfileEntry(instance, key);
            return entry?.Value;
        }

        public CharacterProfileEntry GetProfileEntry(ICharacterInstance instance, string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var profiles = instance?.Info?.Profiles;
            if (profiles == null) return null;
            for (int i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                if (p != null && p.Key == key) return p;
            }
            return null;
        }
    }
}
