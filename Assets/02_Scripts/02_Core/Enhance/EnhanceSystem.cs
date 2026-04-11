using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IEnhanceSystem 구현체. 타입별 전략 위임으로 강화를 처리한다.
    /// </summary>
    public class EnhanceSystem : IEnhanceSystem
    {
        private readonly Dictionary<EnhanceType, IEnhanceStrategy> _strategies = new Dictionary<EnhanceType, IEnhanceStrategy>();
        private readonly IProbabilityModel _probabilityModel;
        private readonly IEventBus _eventBus;
        private readonly IStatSystem _statSystem;

        public EnhanceSystem(IEventBus eventBus, IStatSystem statSystem, IProbabilityModel probabilityModel)
        {
            _eventBus = eventBus;
            _statSystem = statSystem;
            _probabilityModel = probabilityModel;

            Debug.Log("[EnhanceSystem] Init started");
        }

        public void RegisterStrategy(EnhanceType type, IEnhanceStrategy strategy)
        {
            _strategies[type] = strategy;
            Debug.Log($"[EnhanceSystem] Strategy registered: {type}");
        }

        public EnhanceResult Enhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment == null)
            {
                Debug.LogError("[EnhanceSystem] Equipment is null");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                Debug.LogError($"[EnhanceSystem] No strategy for type: {context.Type}");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            if (!strategy.CanEnhance(equipment, context))
            {
                Debug.LogWarning($"[EnhanceSystem] Cannot enhance: {equipment.InstanceId} ({context.Type})");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            _eventBus?.Publish(new EnhanceAttemptEvent
            {
                InstanceId = equipment.InstanceId,
                EnhanceType = context.Type,
                BeforeValue = GetCurrentValue(equipment, context.Type)
            });

            EnhanceCost cost = strategy.GetCost(equipment, context);
            PublishMaterialConsumed(cost, context.Type.ToString());

            EnhanceResult result = strategy.Execute(equipment, context);

            if (result.IsSuccess)
            {
                UpdateEquipmentModifiers(equipment);

                _eventBus?.Publish(new EnhanceSuccessEvent
                {
                    InstanceId = equipment.InstanceId,
                    EnhanceType = context.Type,
                    BeforeValue = result.BeforeValue,
                    AfterValue = result.AfterValue
                });

                Debug.Log($"[EnhanceSystem] Success: {equipment.InstanceId} {context.Type} {result.BeforeValue} → {result.AfterValue}");
            }
            else
            {
                _eventBus?.Publish(new EnhanceFailEvent
                {
                    InstanceId = equipment.InstanceId,
                    EnhanceType = context.Type,
                    CurrentPityCount = equipment.PityCount,
                    MaxPity = result.MaxPity,
                    AppliedPolicy = result.FailPolicy
                });

                Debug.Log($"[EnhanceSystem] Failed: {equipment.InstanceId} {context.Type} (policy: {result.FailPolicy})");
            }

            return result;
        }

        public bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment == null) return false;

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return false;
            }

            return strategy.CanEnhance(equipment, context);
        }

        public EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment == null || !_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return new EnhanceCost { Materials = System.Array.Empty<EnhanceMaterialEntry>(), CanAfford = false };
            }

            return strategy.GetCost(equipment, context);
        }

        public float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment == null) return 0f;

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return 0f;
            }

            return strategy.GetDisplayProbability(equipment, context);
        }

        private void UpdateEquipmentModifiers(EquipmentInstanceData equipment)
        {
            IStatContainer container = _statSystem.GetContainer(equipment.InstanceId);
            if (container == null)
            {
                Debug.LogWarning($"[EnhanceSystem] StatContainer not found for: {equipment.InstanceId}");
                return;
            }

            container.RemoveModifiersFromSource(equipment);

            float levelBonus = equipment.Level;
            float gradeBonus = equipment.Grade * 5f;
            float transcendBonus = equipment.TranscendStep * 3f;

            float atkTotal = levelBonus + gradeBonus + transcendBonus;
            if (atkTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.ATK, StatModType.Flat, atkTotal,
                    StatLayer.Equipment, equipment));
            }

            float defTotal = levelBonus * 0.8f + gradeBonus * 0.6f + transcendBonus * 2f;
            if (defTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.DEF, StatModType.Flat, defTotal,
                    StatLayer.Equipment, equipment));
            }

            float hpTotal = (levelBonus + gradeBonus + transcendBonus) * 10f;
            if (hpTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.MaxHP, StatModType.Flat, hpTotal,
                    StatLayer.Equipment, equipment));
            }

            if (equipment.AwakeningSlots != null)
            {
                foreach (AwakeningSlotData slot in equipment.AwakeningSlots)
                {
                    if (!slot.IsUnlocked || string.IsNullOrEmpty(slot.OptionId)) continue;

                    StatType? statType = MapAwakeningOption(slot.OptionId);
                    if (statType.HasValue)
                    {
                        StatModType modType = (statType == StatType.CritRate || statType == StatType.CritDamage)
                            ? StatModType.Percent : StatModType.Flat;

                        container.AddModifier(new StatModifier(
                            statType.Value, modType, slot.OptionValue,
                            StatLayer.Equipment, equipment));
                    }
                }
            }

            container.RecalculateAll();

            Debug.Log($"[EnhanceSystem] Equipment modifiers updated for: {equipment.InstanceId} (Lv{equipment.Level} G{equipment.Grade} T{equipment.TranscendStep})");
        }

        private void PublishMaterialConsumed(EnhanceCost cost, string reason)
        {
            if (cost.Materials == null) return;

            foreach (EnhanceMaterialEntry entry in cost.Materials)
            {
                _eventBus?.Publish(new MaterialConsumedEvent
                {
                    MaterialType = entry.MaterialType,
                    Amount = entry.Amount,
                    Reason = reason
                });
            }
        }

        private StatType? MapAwakeningOption(string optionId)
        {
            return optionId switch
            {
                "ATK_FLAT" => StatType.ATK,
                "DEF_FLAT" => StatType.DEF,
                "HP_FLAT" => StatType.MaxHP,
                "CRIT_RATE" => StatType.CritRate,
                "CRIT_DMG" => StatType.CritDamage,
                _ => null
            };
        }

        // NOTE: 새 EnhanceType 추가 시 이 switch에 케이스를 추가해야 합니다.
        // 향후 리팩터링 시 IEnhanceStrategy에 GetCurrentValue()를 위임하는 것을 고려하세요.
        private int GetCurrentValue(EquipmentInstanceData equipment, EnhanceType type)
        {
            return type switch
            {
                EnhanceType.Level => equipment.Level,
                EnhanceType.Grade => equipment.Grade,
                EnhanceType.Transcend => equipment.TranscendStep,
                EnhanceType.Awakening => 0,
                _ => 0
            };
        }
    }
}
