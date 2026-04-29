using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 각성 기본 전략. 가중치 랜덤 옵션 선택 + 슬롯 잠금 지원.
    /// </summary>
    public class DefaultAwakeningStrategy : IEnhanceStrategy
    {
        private readonly EnhanceDataCollection _collection;
        private readonly IEventBus _eventBus;

        public DefaultAwakeningStrategy(EnhanceDataCollection collection, IEventBus eventBus)
        {
            _collection = collection;
            _eventBus = eventBus;
        }

        public EnhanceResult Execute(IEnhanceable target, EnhanceContext context)
        {
            int slotIndex = context.TargetSlotIndex;
            AwakeningSlotData slot = target.AwakeningSlots[slotIndex];

            AwakeningOptionEntry option = RollOption();
            float rolledValue = Random.Range(option.MinValue, option.MaxValue);

            slot.OptionId = option.OptionId;
            slot.OptionValue = rolledValue;

            _eventBus?.Publish(new AwakeningCompleteEvent
            {
                InstanceId = target.InstanceId,
                SlotIndex = slotIndex,
                OptionId = option.OptionId,
                OptionValue = rolledValue
            });

            Debug.Log($"[Awakening] Slot {slotIndex}: {option.OptionId} ({rolledValue:F2} [{option.MinValue}~{option.MaxValue}])");

            return new EnhanceResult
            {
                IsSuccess = true,
                Type = EnhanceType.Awakening,
                BeforeValue = slotIndex,
                AfterValue = slotIndex,
                FailPolicy = EnhanceFailPolicy.Keep
            };
        }

        public bool CanEnhance(IEnhanceable target, EnhanceContext context)
        {
            if (target.AwakeningSlots == null || target.AwakeningSlots.Length == 0)
            {
                Debug.LogWarning("[Awakening] No awakening slots");
                return false;
            }

            int slotIndex = context.TargetSlotIndex;

            if (slotIndex < 0 || slotIndex >= target.AwakeningSlots.Length)
            {
                Debug.LogWarning($"[Awakening] Invalid slot index: {slotIndex}");
                return false;
            }

            AwakeningSlotData slot = target.AwakeningSlots[slotIndex];

            if (!slot.IsUnlocked)
            {
                Debug.LogWarning($"[Awakening] Slot {slotIndex} is locked");
                return false;
            }

            if (slot.IsLocked)
            {
                Debug.LogWarning($"[Awakening] Slot {slotIndex} option is locked by user");
                return false;
            }

            return true;
        }

        public EnhanceCost GetCost(IEnhanceable target, EnhanceContext context)
        {
            EnhanceData awakeningData = _collection != null ? _collection.Find(EnhanceType.Awakening) : null;
            int costBase = awakeningData != null ? awakeningData.AwakeningCostBase : 0;
            int cost = costBase * (1 + context.TargetSlotIndex);

            return new EnhanceCost
            {
                Materials = new[]
                {
                    new EnhanceMaterialEntry
                    {
                        MaterialType = EnhanceMaterialType.AwakeningStone,
                        Amount = cost
                    }
                },
                CanAfford = true
            };
        }

        public float GetDisplayProbability(IEnhanceable target, EnhanceContext context)
        {
            return 1f;
        }

        private AwakeningOptionEntry RollOption()
        {
            EnhanceData awakeningData = _collection != null ? _collection.Find(EnhanceType.Awakening) : null;
            IReadOnlyList<AwakeningOptionEntry> options = awakeningData != null ? awakeningData.AwakeningOptions : null;

            if (options == null || options.Count == 0)
            {
                Debug.LogError("[Awakening] No options configured");
                return new AwakeningOptionEntry { OptionId = "NONE", MinValue = 0f, MaxValue = 0f, Weight = 0 };
            }

            int totalWeight = 0;
            for (int i = 0; i < options.Count; i++)
            {
                totalWeight += options[i].Weight;
            }

            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;

            for (int i = 0; i < options.Count; i++)
            {
                accumulated += options[i].Weight;
                if (roll < accumulated)
                {
                    return options[i];
                }
            }

            return options[options.Count - 1];
        }
    }
}
