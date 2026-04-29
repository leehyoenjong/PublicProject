using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IEnhanceSystem 구현체. 타입별 전략 위임으로 강화를 처리한다.
    /// 대상은 IEnhanceable — 장비/캐릭터/펫 모두 동일 흐름으로 통과.
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

        public EnhanceResult Enhance(IEnhanceable target, EnhanceContext context)
        {
            if (target == null)
            {
                Debug.LogError("[EnhanceSystem] Target is null");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                Debug.LogError($"[EnhanceSystem] No strategy for type: {context.Type}");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            if (!strategy.CanEnhance(target, context))
            {
                Debug.LogWarning($"[EnhanceSystem] Cannot enhance: {target.InstanceId} ({context.Type})");
                return new EnhanceResult { IsSuccess = false, Type = context.Type };
            }

            _eventBus?.Publish(new EnhanceAttemptEvent
            {
                InstanceId = target.InstanceId,
                EnhanceType = context.Type,
                BeforeValue = GetCurrentValue(target, context.Type)
            });

            EnhanceCost cost = strategy.GetCost(target, context);
            PublishMaterialConsumed(cost, context.Type.ToString());

            EnhanceResult result = strategy.Execute(target, context);

            if (result.IsSuccess)
            {
                UpdateModifiers(target);

                _eventBus?.Publish(new EnhanceSuccessEvent
                {
                    InstanceId = target.InstanceId,
                    EnhanceType = context.Type,
                    BeforeValue = result.BeforeValue,
                    AfterValue = result.AfterValue
                });

                Debug.Log($"[EnhanceSystem] Success: {target.InstanceId} {context.Type} {result.BeforeValue} → {result.AfterValue}");
            }
            else
            {
                _eventBus?.Publish(new EnhanceFailEvent
                {
                    InstanceId = target.InstanceId,
                    EnhanceType = context.Type,
                    CurrentPityCount = target.PityCount,
                    MaxPity = result.MaxPity,
                    AppliedPolicy = result.FailPolicy
                });

                Debug.Log($"[EnhanceSystem] Failed: {target.InstanceId} {context.Type} (policy: {result.FailPolicy})");
            }

            return result;
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            if (target == null) return false;

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return false;
            }

            return strategy.CanEnhance(target, context);
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            if (target == null || !_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return new EnhanceCost { Materials = System.Array.Empty<EnhanceMaterialEntry>(), CanAfford = false };
            }

            return strategy.GetCost(target, context);
        }

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            if (target == null) return 0f;

            if (!_strategies.TryGetValue(context.Type, out IEnhanceStrategy strategy))
            {
                return 0f;
            }

            return strategy.GetDisplayProbability(target, context);
        }

        private void UpdateModifiers(IEnhanceable target)
        {
            IStatContainer container = _statSystem.GetContainer(target.InstanceId);
            if (container == null)
            {
                Debug.LogWarning($"[EnhanceSystem] StatContainer not found for: {target.InstanceId}");
                return;
            }

            container.RemoveModifiersFromSource(target);

            float levelBonus = target.Level;
            float gradeBonus = target.Grade * 5f;
            float transcendBonus = target.TranscendStep * 3f;
            float evolutionBonus = target.EvolutionStage * 10f;

            float atkTotal = levelBonus + gradeBonus + transcendBonus + evolutionBonus;
            if (atkTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.Attack, StatLayer.Flat, atkTotal,
                    source: target, sourceTag: ModifierSource.Equipment));
            }

            float defTotal = levelBonus * 0.8f + gradeBonus * 0.6f + transcendBonus * 2f + evolutionBonus * 1.5f;
            if (defTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.Defense, StatLayer.Flat, defTotal,
                    source: target, sourceTag: ModifierSource.Equipment));
            }

            float hpTotal = (levelBonus + gradeBonus + transcendBonus + evolutionBonus) * 10f;
            if (hpTotal > 0f)
            {
                container.AddModifier(new StatModifier(
                    StatType.HP, StatLayer.Flat, hpTotal,
                    source: target, sourceTag: ModifierSource.Equipment));
            }

            if (target.AwakeningSlots != null)
            {
                foreach (AwakeningSlotData slot in target.AwakeningSlots)
                {
                    if (!slot.IsUnlocked || string.IsNullOrEmpty(slot.OptionId)) continue;

                    StatType? statType = MapAwakeningOption(slot.OptionId);
                    if (statType.HasValue)
                    {
                        StatLayer layer = (statType == StatType.CritRate || statType == StatType.CritDamage)
                            ? StatLayer.Percent : StatLayer.Flat;

                        container.AddModifier(new StatModifier(
                            statType.Value, layer, slot.OptionValue,
                            source: target, sourceTag: ModifierSource.Equipment));
                    }
                }
            }

            container.RecalculateAll();

            Debug.Log($"[EnhanceSystem] Modifiers updated for: {target.InstanceId} (Lv{target.Level} G{target.Grade} T{target.TranscendStep})");
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
                "ATK_FLAT" => StatType.Attack,
                "DEF_FLAT" => StatType.Defense,
                "HP_FLAT" => StatType.HP,
                "CRIT_RATE" => StatType.CritRate,
                "CRIT_DMG" => StatType.CritDamage,
                _ => null
            };
        }

        // NOTE: 새 EnhanceType 추가 시 이 switch에 케이스를 추가해야 합니다.
        // 향후 리팩터링 시 IEnhanceStrategy에 GetCurrentValue()를 위임하는 것을 고려하세요.
        private int GetCurrentValue(IEnhanceable target, EnhanceType type)
        {
            return type switch
            {
                EnhanceType.Level => target.Level,
                EnhanceType.Grade => target.Grade,
                EnhanceType.Transcend => target.TranscendStep,
                EnhanceType.Evolution => target.EvolutionStage,
                EnhanceType.Awakening => 0,
                _ => 0
            };
        }
    }
}
