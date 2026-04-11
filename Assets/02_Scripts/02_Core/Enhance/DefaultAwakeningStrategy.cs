using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 각성 기본 전략. 가중치 랜덤 옵션 선택 + 슬롯 잠금 지원.
    /// </summary>
    public class DefaultAwakeningStrategy : IEnhanceStrategy
    {
        private readonly EnhanceConfig _config;
        private readonly IEventBus _eventBus;

        public DefaultAwakeningStrategy(EnhanceConfig config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public EnhanceResult Execute(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int slotIndex = context.TargetSlotIndex;
            AwakeningSlotData slot = equipment.AwakeningSlots[slotIndex];

            AwakeningOptionEntry option = RollOption();
            float rolledValue = Random.Range(option.MinValue, option.MaxValue);

            slot.OptionId = option.OptionId;
            slot.OptionValue = rolledValue;

            _eventBus?.Publish(new AwakeningCompleteEvent
            {
                InstanceId = equipment.InstanceId,
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

        public bool CanEnhance(EquipmentInstanceData equipment, EnhanceContext context)
        {
            if (equipment.AwakeningSlots == null || equipment.AwakeningSlots.Length == 0)
            {
                Debug.LogWarning("[Awakening] No awakening slots");
                return false;
            }

            int slotIndex = context.TargetSlotIndex;

            if (slotIndex < 0 || slotIndex >= equipment.AwakeningSlots.Length)
            {
                Debug.LogWarning($"[Awakening] Invalid slot index: {slotIndex}");
                return false;
            }

            AwakeningSlotData slot = equipment.AwakeningSlots[slotIndex];

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

        public EnhanceCost GetCost(EquipmentInstanceData equipment, EnhanceContext context)
        {
            int cost = _config.GetAwakeningCost(context.TargetSlotIndex);

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

        public float GetDisplayProbability(EquipmentInstanceData equipment, EnhanceContext context)
        {
            return 1f;
        }

        private AwakeningOptionEntry RollOption()
        {
            AwakeningOptionEntry[] options = _config.GetAwakeningOptions();

            if (options == null || options.Length == 0)
            {
                Debug.LogError("[Awakening] No options configured");
                return new AwakeningOptionEntry { OptionId = "NONE", MinValue = 0f, MaxValue = 0f, Weight = 0 };
            }

            int totalWeight = 0;
            foreach (AwakeningOptionEntry option in options)
            {
                totalWeight += option.Weight;
            }

            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;

            foreach (AwakeningOptionEntry option in options)
            {
                accumulated += option.Weight;
                if (roll < accumulated)
                {
                    return option;
                }
            }

            return options[options.Length - 1];
        }
    }
}
