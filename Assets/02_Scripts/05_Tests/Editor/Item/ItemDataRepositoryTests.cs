using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Item
{
    public class ItemDataRepositoryTests
    {
        private ItemData _gold;
        private ItemData _potion;
        private ItemDataCollection _collection;

        [SetUp]
        public void SetUp()
        {
            _gold = TestHelpers.MakeItemData(40001, StackType.Stack, 999999, ItemCategory.Currency);
            _potion = TestHelpers.MakeItemData(10001, StackType.Stack, 99, ItemCategory.Consumable);
            _collection = ScriptableObject.CreateInstance<ItemDataCollection>();
            _collection.SetItems(new ItemData[] { _gold, _potion });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gold);
            Object.DestroyImmediate(_potion);
            Object.DestroyImmediate(_collection);
        }

        [Test]
        public void Initialize_ValidCollection_LoadsAll()
        {
            ItemDataRepository repo = new ItemDataRepository(_collection);
            Assert.AreEqual(2, repo.GetAll().Count);
        }

        [Test]
        public void TryGetItem_RegisteredMID_ReturnsTrue()
        {
            ItemDataRepository repo = new ItemDataRepository(_collection);
            bool found = repo.TryGetItem(40001, out IItem item);
            Assert.IsTrue(found);
            Assert.AreEqual(40001, item.MID);
            Assert.AreEqual(ItemCategory.Currency, item.Category);
        }

        [Test]
        public void TryGetItem_UnknownMID_ReturnsFalse()
        {
            ItemDataRepository repo = new ItemDataRepository(_collection);
            Assert.IsFalse(repo.TryGetItem(99999, out _));
        }

        [Test]
        public void Initialize_NullCollection_DoesNotThrow()
        {
            ItemDataRepository repo = new ItemDataRepository(null);
            Assert.AreEqual(0, repo.GetAll().Count);
        }

        [Test]
        public void GetItem_RegisteredMID_ReturnsItem()
        {
            ItemDataRepository repo = new ItemDataRepository(_collection);
            IItem item = repo.GetItem(10001);
            Assert.IsNotNull(item);
            Assert.AreEqual(10001, item.MID);
        }
    }
}
