using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultAwakeningStrategyTests
    {
        private EnhanceDataCollection _collection;
        private FakeEventBus _eventBus;
        private DefaultAwakeningStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _collection = TestHelpers.MakeDefaultEnhanceCollection();
            _eventBus = new FakeEventBus();
            _strategy = new DefaultAwakeningStrategy(_collection, _eventBus);
            Random.InitState(42);
        }

        private EquipmentInstanceData MakeEquipment(AwakeningSlotData[] slots)
        {
            return new EquipmentInstanceData { InstanceId = "eq1", AwakeningSlots = slots };
        }

        private AwakeningSlotData MakeSlot(int index, bool unlocked = true, bool optionLocked = false)
        {
            return new AwakeningSlotData { SlotIndex = index, IsUnlocked = unlocked, IsLocked = optionLocked };
        }

        [Test]
        public void CanEnhance_NullSlots_ReturnsFalse()
        {
            var eq = new EquipmentInstanceData { AwakeningSlots = null };
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 0 }));
        }

        [Test]
        public void CanEnhance_EmptySlots_ReturnsFalse()
        {
            var eq = MakeEquipment(System.Array.Empty<AwakeningSlotData>());
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 0 }));
        }

        [Test]
        public void CanEnhance_InvalidSlotIndex_ReturnsFalse()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0) });
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 5 }));
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = -1 }));
        }

        [Test]
        public void CanEnhance_SlotNotUnlocked_ReturnsFalse()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0, unlocked: false) });
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 0 }));
        }

        [Test]
        public void CanEnhance_OptionLocked_ReturnsFalse()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0, unlocked: true, optionLocked: true) });
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 0 }));
        }

        [Test]
        public void CanEnhance_UnlockedFree_ReturnsTrue()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0) });
            Assert.IsTrue(_strategy.CanEnhance(eq, new EnhanceContext { TargetSlotIndex = 0 }));
        }

        [Test]
        public void Execute_AssignsOptionToSlot()
        {
            var slot = MakeSlot(0);
            var eq = MakeEquipment(new[] { slot });

            EnhanceResult result = _strategy.Execute(eq, new EnhanceContext { TargetSlotIndex = 0 });

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotEmpty(slot.OptionId);
            Assert.That(slot.OptionValue, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void Execute_PublishesAwakeningCompleteEvent()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0), MakeSlot(1) });

            _strategy.Execute(eq, new EnhanceContext { TargetSlotIndex = 1 });

            var events = _eventBus.GetPublished<AwakeningCompleteEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(1, events[0].SlotIndex);
            Assert.AreEqual("eq1", events[0].InstanceId);
        }

        [Test]
        public void GetCost_ReturnsAwakeningStoneWithSlotMultiplier()
        {
            var eq = MakeEquipment(new[] { MakeSlot(0) });

            EnhanceCost slot0 = _strategy.GetCost(eq, new EnhanceContext { TargetSlotIndex = 0 });
            EnhanceCost slot1 = _strategy.GetCost(eq, new EnhanceContext { TargetSlotIndex = 1 });

            Assert.AreEqual(EnhanceMaterialType.AwakeningStone, slot0.Materials[0].MaterialType);
            Assert.AreEqual(20, slot0.Materials[0].Amount); // base*(1+0)
            Assert.AreEqual(40, slot1.Materials[0].Amount); // base*(1+1)
        }

        [Test]
        public void Execute_OptionIsFromConfigTable()
        {
            var validOptions = new System.Collections.Generic.HashSet<string>
            {
                "ATK_FLAT", "DEF_FLAT", "HP_FLAT", "CRIT_RATE", "CRIT_DMG"
            };

            for (int i = 0; i < 30; i++)
            {
                var slot = MakeSlot(0);
                var eq = MakeEquipment(new[] { slot });
                _strategy.Execute(eq, new EnhanceContext { TargetSlotIndex = 0 });
                Assert.Contains(slot.OptionId, new System.Collections.Generic.List<string>(validOptions));
            }
        }
    }
}
