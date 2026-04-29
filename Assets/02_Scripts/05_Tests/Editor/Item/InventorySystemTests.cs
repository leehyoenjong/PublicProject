using System;
using NUnit.Framework;

namespace PublicFramework.Tests.Item
{
    public class InventorySystemTests
    {
        private FakeItemRepository _repo;
        private FakeEventBus _bus;
        private InventorySystem _inventory;

        [SetUp]
        public void SetUp()
        {
            _repo = new FakeItemRepository();
            _bus = new FakeEventBus();
            _inventory = new InventorySystem(_repo, _bus);
        }

        private ItemData Register(
            int mid,
            StackType stackType = StackType.Stack,
            int maxStack = 99,
            ItemCategory category = ItemCategory.Consumable,
            int convertRewardMID = 0,
            int convertRewardCount = 0)
        {
            ItemData data = TestHelpers.MakeItemData(mid, stackType, maxStack, category, convertRewardMID, convertRewardCount);
            _repo.Register(data);
            return data;
        }

        // ---------- AddItem: Stack ----------

        [Test]
        public void AddItem_Stack_AccumulatesCount()
        {
            Register(1001, StackType.Stack, maxStack: 99);

            _inventory.AddItem(1001, 3, null);
            _inventory.AddItem(1001, 5, null);

            Assert.AreEqual(8, _inventory.GetCount(1001));
        }

        [Test]
        public void AddItem_Stack_RespectsMaxStack()
        {
            Register(1001, StackType.Stack, maxStack: 10);

            ItemAddResult result = _inventory.AddItem(1001, 15, null);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(10, result.AddedCount);
            Assert.AreEqual(10, _inventory.GetCount(1001));
        }

        [Test]
        public void AddItem_Stack_PublishesAcquiredEvent()
        {
            Register(1001, StackType.Stack);

            _inventory.AddItem(1001, 4, "testSource");

            var events = _bus.GetPublished<ItemAcquiredEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(1001, events[0].MID);
            Assert.AreEqual(4, events[0].Count);
            Assert.AreEqual("testSource", events[0].Source);
        }

        [Test]
        public void AddItem_ZeroOrNegativeCount_ReturnsFailure()
        {
            Register(1001, StackType.Stack);

            ItemAddResult zero = _inventory.AddItem(1001, 0, null);
            ItemAddResult negative = _inventory.AddItem(1001, -3, null);

            Assert.IsFalse(zero.Success);
            Assert.IsFalse(negative.Success);
            Assert.AreEqual(0, _inventory.GetCount(1001));
        }

        [Test]
        public void AddItem_UnknownMID_ReturnsFailure()
        {
            ItemAddResult result = _inventory.AddItem(9999, 1, null);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _bus.GetPublished<ItemAcquiredEvent>().Count);
        }

        // ---------- AddItem: Instance ----------

        [Test]
        public void AddItem_Instance_CreatesMultipleInstances()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);

            _inventory.AddItem(2001, 3, null);

            Assert.AreEqual(3, _inventory.GetAll().Count);
            Assert.AreEqual(3, _inventory.GetCount(2001));
        }

        [Test]
        public void AddItem_Instance_PublishesEventPerInstance()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);

            _inventory.AddItem(2001, 3, null);

            var events = _bus.GetPublished<ItemAcquiredEvent>();
            Assert.AreEqual(3, events.Count);
            foreach (ItemAcquiredEvent e in events) Assert.AreEqual(1, e.Count);
        }

        // ---------- AddItem: Convert ----------

        [Test]
        public void AddItem_Convert_FirstAcquisition_Succeeds()
        {
            Register(3001, StackType.Convert, convertRewardMID: 4001, convertRewardCount: 10);
            Register(4001, StackType.Stack);

            ItemAddResult result = _inventory.AddItem(3001, 1, null);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.AddedCount);
            Assert.AreEqual(0, result.ConvertedItems.Count);
            Assert.AreEqual(1, _inventory.GetCount(3001));
        }

        [Test]
        public void AddItem_Convert_Duplicate_AddsRewardAndPublishesConverted()
        {
            Register(3001, StackType.Convert, convertRewardMID: 4001, convertRewardCount: 10);
            Register(4001, StackType.Stack);

            _inventory.AddItem(3001, 1, null);
            ItemAddResult duplicate = _inventory.AddItem(3001, 1, null);

            Assert.IsTrue(duplicate.Success);
            Assert.AreEqual(1, duplicate.ConvertedItems.Count);
            Assert.AreEqual(4001, duplicate.ConvertedItems[0].MID);
            Assert.AreEqual(10, duplicate.ConvertedItems[0].Count);
            Assert.AreEqual(10, _inventory.GetCount(4001));

            var convertedEvents = _bus.GetPublished<ItemConvertedEvent>();
            Assert.AreEqual(1, convertedEvents.Count);
            Assert.AreEqual(3001, convertedEvents[0].OriginalMID);
            Assert.AreEqual(4001, convertedEvents[0].ConvertedMID);
        }

        [Test]
        public void AddItem_Convert_ZeroRewardSetting_SkipsConversion()
        {
            Register(3001, StackType.Convert, convertRewardMID: 0, convertRewardCount: 0);

            _inventory.AddItem(3001, 1, null);
            _inventory.AddItem(3001, 1, null); // 중복이지만 보상 없음

            Assert.AreEqual(1, _inventory.GetCount(3001));
            Assert.AreEqual(0, _bus.GetPublished<ItemConvertedEvent>().Count);
        }

        // ---------- Consume ----------

        [Test]
        public void ConsumeByMID_DeductsStackCount()
        {
            Register(1001, StackType.Stack);
            _inventory.AddItem(1001, 10, null);

            bool ok = _inventory.ConsumeByMID(1001, 3);

            Assert.IsTrue(ok);
            Assert.AreEqual(7, _inventory.GetCount(1001));
        }

        [Test]
        public void ConsumeByMID_ExhaustsStack_RemovesInstance()
        {
            Register(1001, StackType.Stack);
            _inventory.AddItem(1001, 5, null);

            _inventory.ConsumeByMID(1001, 5);

            Assert.AreEqual(0, _inventory.GetCount(1001));
            Assert.AreEqual(0, _inventory.GetAll().Count);
        }

        [Test]
        public void ConsumeByMID_InsufficientCount_Fails()
        {
            Register(1001, StackType.Stack);
            _inventory.AddItem(1001, 2, null);

            bool ok = _inventory.ConsumeByMID(1001, 5);

            Assert.IsFalse(ok);
            Assert.AreEqual(2, _inventory.GetCount(1001));
        }

        [Test]
        public void ConsumeByMID_PublishesConsumedEvent()
        {
            Register(1001, StackType.Stack);
            _inventory.AddItem(1001, 5, null);
            _bus.Clear();

            _inventory.ConsumeByMID(1001, 2);

            var events = _bus.GetPublished<ItemConsumedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(1001, events[0].MID);
            Assert.AreEqual(2, events[0].Count);
        }

        [Test]
        public void ConsumeByInstance_Succeeds()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            ItemAddResult add = _inventory.AddItem(2001, 1, null);

            bool ok = _inventory.ConsumeByInstance(add.InstanceId, 1);

            Assert.IsTrue(ok);
            Assert.IsNull(_inventory.GetInstance(add.InstanceId));
        }

        // ---------- Bound ----------

        [Test]
        public void SetBound_ExistingInstance_SetsIsBoundTrue()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            ItemAddResult add = _inventory.AddItem(2001, 1, null);

            bool ok = _inventory.SetBound(add.InstanceId);

            Assert.IsTrue(ok);
            Assert.IsTrue(_inventory.GetInstance(add.InstanceId).IsBound);
        }

        [Test]
        public void SetBound_UnknownInstance_Fails()
        {
            Assert.IsFalse(_inventory.SetBound("does_not_exist"));
        }

        // ---------- Expire ----------

        [Test]
        public void PurgeExpired_RemovesExpiredInstances()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            var expired = new ItemInstance("expired_1", 2001, 1,
                DateTime.UtcNow.AddDays(-1), expireAt: DateTime.UtcNow.AddMinutes(-1));
            TestHelpers.InjectInventoryInstance(_inventory, expired);

            int purged = _inventory.PurgeExpired();

            Assert.AreEqual(1, purged);
            Assert.IsNull(_inventory.GetInstance("expired_1"));
        }

        [Test]
        public void PurgeExpired_PublishesExpiredEvent()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            var expired = new ItemInstance("expired_1", 2001, 1,
                DateTime.UtcNow.AddDays(-1), expireAt: DateTime.UtcNow.AddMinutes(-1));
            TestHelpers.InjectInventoryInstance(_inventory, expired);

            _inventory.PurgeExpired();

            var events = _bus.GetPublished<ItemExpiredEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(2001, events[0].MID);
            Assert.AreEqual("expired_1", events[0].InstanceId);
        }

        [Test]
        public void PurgeExpired_PreservesNotExpired()
        {
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            var stillValid = new ItemInstance("valid_1", 2001, 1,
                DateTime.UtcNow, expireAt: DateTime.UtcNow.AddHours(1));
            TestHelpers.InjectInventoryInstance(_inventory, stillValid);

            int purged = _inventory.PurgeExpired();

            Assert.AreEqual(0, purged);
            Assert.IsNotNull(_inventory.GetInstance("valid_1"));
        }

        [Test]
        public void PurgeExpired_EmptyInventory_ReturnsZero()
        {
            Assert.AreEqual(0, _inventory.PurgeExpired());
            Assert.AreEqual(0, _bus.GetPublished<ItemExpiredEvent>().Count);
        }

        // ---------- Query ----------

        [Test]
        public void GetByCategory_FiltersByCategory()
        {
            Register(1001, StackType.Stack, category: ItemCategory.Consumable);
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            _inventory.AddItem(1001, 5, null);
            _inventory.AddItem(2001, 2, null);

            var equipment = _inventory.GetByCategory(ItemCategory.Equipment);

            Assert.AreEqual(2, equipment.Count);
            foreach (IItemInstance inst in equipment) Assert.AreEqual(2001, inst.MID);
        }

        [Test]
        public void GetInstance_UnknownId_ReturnsNull()
        {
            Assert.IsNull(_inventory.GetInstance("missing"));
        }

        [Test]
        public void GetAll_ReturnsAllInstances()
        {
            Register(1001, StackType.Stack);
            Register(2001, StackType.Instance, category: ItemCategory.Equipment);
            _inventory.AddItem(1001, 5, null);
            _inventory.AddItem(2001, 2, null);

            Assert.AreEqual(3, _inventory.GetAll().Count); // 1 stack + 2 instance
        }
    }
}
