using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStatContainer 구현체
    /// 4레이어 계산: Final = (Base + Growth + EquipFlat + BuffFlat) × (1 + EquipPercent + BuffPercent)
    /// </summary>
    public class StatContainer : IStatContainer
    {
        private readonly string _ownerId;
        private readonly IEventBus _eventBus;

        private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _growthValues = new Dictionary<StatType, float>();
        private readonly List<IStatModifier> _equipmentModifiers = new List<IStatModifier>();
        private readonly List<IStatModifier> _buffModifiers = new List<IStatModifier>();
        private readonly Dictionary<StatType, float> _cachedFinalValues = new Dictionary<StatType, float>();

        public StatContainer(string ownerId, IEventBus eventBus)
        {
            _ownerId = ownerId;
            _eventBus = eventBus;

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                _baseValues[type] = 0f;
                _growthValues[type] = 0f;
                _cachedFinalValues[type] = 0f;
            }

            Debug.Log($"[StatContainer] Created for owner: {_ownerId}");
        }

        public float GetFinalValue(StatType type)
        {
            return _cachedFinalValues.TryGetValue(type, out float value) ? value : 0f;
        }

        public float GetBaseValue(StatType type)
        {
            return _baseValues.TryGetValue(type, out float value) ? value : 0f;
        }

        public void SetBaseValue(StatType type, float value)
        {
            float oldFinal = GetFinalValue(type);
            _baseValues[type] = value;
            RecalculateStat(type, oldFinal);
        }

        public void SetGrowthValue(StatType type, float value)
        {
            float oldFinal = GetFinalValue(type);
            _growthValues[type] = value;
            RecalculateStat(type, oldFinal);
        }

        public void AddModifier(IStatModifier modifier)
        {
            if (modifier == null)
            {
                Debug.LogWarning("[StatContainer] Null modifier ignored");
                return;
            }

            if (modifier.Layer == StatLayer.Base || modifier.Layer == StatLayer.Growth)
            {
                Debug.LogWarning("[StatContainer] Base/Growth 레이어는 SetBaseValue/SetGrowthValue를 사용하세요. Modifier 무시됨.");
                return;
            }

            float oldFinal = GetFinalValue(modifier.TargetStat);

            List<IStatModifier> list = GetModifierList(modifier.Layer);
            list.Add(modifier);
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            RecalculateStat(modifier.TargetStat, oldFinal);

            _eventBus?.Publish(new ModifierAddedEvent
            {
                OwnerId = _ownerId,
                Type = modifier.TargetStat,
                ModType = modifier.ModType,
                Value = modifier.Value,
                Layer = modifier.Layer
            });

            Debug.Log($"[StatContainer] Modifier added: {modifier.TargetStat} {modifier.ModType} {modifier.Value} ({modifier.Layer})");
        }

        public void RemoveModifier(IStatModifier modifier)
        {
            if (modifier == null) return;

            float oldFinal = GetFinalValue(modifier.TargetStat);

            List<IStatModifier> list = GetModifierList(modifier.Layer);
            if (!list.Remove(modifier)) return;

            RecalculateStat(modifier.TargetStat, oldFinal);

            _eventBus?.Publish(new ModifierRemovedEvent
            {
                OwnerId = _ownerId,
                Type = modifier.TargetStat,
                Layer = modifier.Layer
            });

            Debug.Log($"[StatContainer] Modifier removed: {modifier.TargetStat} ({modifier.Layer})");
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (source == null) return 0;

            int count = 0;
            HashSet<StatType> affectedStats = new HashSet<StatType>();

            count += RemoveFromList(_equipmentModifiers, source, affectedStats);
            count += RemoveFromList(_buffModifiers, source, affectedStats);

            foreach (StatType type in affectedStats)
            {
                float oldFinal = GetFinalValue(type);
                RecalculateStat(type, oldFinal);
            }

            if (count > 0)
            {
                Debug.Log($"[StatContainer] Removed {count} modifiers from source");
            }

            return count;
        }

        public IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer)
        {
            return layer switch
            {
                StatLayer.Equipment => _equipmentModifiers.AsReadOnly(),
                StatLayer.Buff => _buffModifiers.AsReadOnly(),
                _ => new List<IStatModifier>().AsReadOnly()
            };
        }

        public void RecalculateAll()
        {
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float oldFinal = GetFinalValue(type);
                RecalculateStat(type, oldFinal);
            }
        }

        private void RecalculateStat(StatType type, float oldFinal)
        {
            float baseValue = _baseValues.TryGetValue(type, out float b) ? b : 0f;
            float growthValue = _growthValues.TryGetValue(type, out float g) ? g : 0f;

            float equipFlat = 0f;
            float equipPercent = 0f;
            float buffFlat = 0f;
            float buffPercent = 0f;

            foreach (IStatModifier mod in _equipmentModifiers)
            {
                if (mod.TargetStat != type) continue;
                if (mod.ModType == StatModType.Flat) equipFlat += mod.Value;
                else equipPercent += mod.Value;
            }

            foreach (IStatModifier mod in _buffModifiers)
            {
                if (mod.TargetStat != type) continue;
                if (mod.ModType == StatModType.Flat) buffFlat += mod.Value;
                else buffPercent += mod.Value;
            }

            float finalValue = (baseValue + growthValue + equipFlat + buffFlat)
                             * (1f + equipPercent + buffPercent);

            finalValue = Mathf.Max(0f, finalValue);
            _cachedFinalValues[type] = finalValue;

            if (Math.Abs(oldFinal - finalValue) > float.Epsilon)
            {
                _eventBus?.Publish(new StatChangedEvent
                {
                    OwnerId = _ownerId,
                    Type = type,
                    OldValue = oldFinal,
                    NewValue = finalValue
                });
            }
        }

        private List<IStatModifier> GetModifierList(StatLayer layer)
        {
            return layer switch
            {
                StatLayer.Equipment => _equipmentModifiers,
                StatLayer.Buff => _buffModifiers,
                _ => throw new System.ArgumentException($"[StatContainer] Modifier는 Equipment/Buff 레이어만 지원합니다: {layer}")
            };
        }

        private int RemoveFromList(List<IStatModifier> list, object source, HashSet<StatType> affectedStats)
        {
            int count = 0;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Source == source)
                {
                    affectedStats.Add(list[i].TargetStat);
                    list.RemoveAt(i);
                    count++;
                }
            }

            return count;
        }
    }
}
