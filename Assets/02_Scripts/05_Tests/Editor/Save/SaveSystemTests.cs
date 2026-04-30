using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Save
{
    public class SaveSystemTests
    {
        [Serializable]
        public class TestData
        {
            public int Value;
            public string Text;
        }

        private FakeDataSerializer _serializer;
        private FakeSaveStorage _storage;
        private FakeDataEncryptor _encryptor;
        private SaveSystem _system;

        [SetUp]
        public void SetUp()
        {
            _serializer = new FakeDataSerializer();
            _storage = new FakeSaveStorage();
            _encryptor = new FakeDataEncryptor();
            _system = new SaveSystem(_serializer, _storage);
        }

        // ---------- 생성자 ----------

        [Test]
        public void Constructor_NullSerializer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveSystem(null, _storage));
        }

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveSystem(_serializer, null));
        }

        [Test]
        public void Constructor_NullEncryptor_AllowedAsOptional()
        {
            Assert.DoesNotThrow(() => new SaveSystem(_serializer, _storage, null));
        }

        // ---------- Save / Load ----------

        [Test]
        public void Save_AndLoad_RoundtripsValue()
        {
            _system.Save(0, "key", new TestData { Value = 42, Text = "hello" });

            TestData loaded = _system.Load<TestData>(0, "key");

            Assert.AreEqual(42, loaded.Value);
            Assert.AreEqual("hello", loaded.Text);
        }

        [Test]
        public void Load_NotSaved_ReturnsDefault()
        {
            TestData loaded = _system.Load<TestData>(0, "missing");
            Assert.IsNull(loaded);
        }

        [Test]
        public void Save_OverwritesExistingValue()
        {
            _system.Save(0, "key", new TestData { Value = 1 });
            _system.Save(0, "key", new TestData { Value = 2 });

            Assert.AreEqual(2, _system.Load<TestData>(0, "key").Value);
        }

        [Test]
        public void Save_DifferentSlots_IndependentStorage()
        {
            _system.Save(0, "key", new TestData { Value = 10 });
            _system.Save(1, "key", new TestData { Value = 20 });

            Assert.AreEqual(10, _system.Load<TestData>(0, "key").Value);
            Assert.AreEqual(20, _system.Load<TestData>(1, "key").Value);
        }

        // ---------- HasKey / DeleteKey ----------

        [Test]
        public void HasKey_AfterSave_ReturnsTrue()
        {
            _system.Save(0, "key", new TestData());
            Assert.IsTrue(_system.HasKey(0, "key"));
        }

        [Test]
        public void HasKey_NotSaved_ReturnsFalse()
        {
            Assert.IsFalse(_system.HasKey(0, "nope"));
        }

        [Test]
        public void DeleteKey_RemovesValue()
        {
            _system.Save(0, "key", new TestData { Value = 1 });
            _system.DeleteKey(0, "key");

            Assert.IsFalse(_system.HasKey(0, "key"));
        }

        // ---------- ValidateSlotIndex ----------

        [Test]
        public void Save_InvalidSlot_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _system.Save(99, "k", new TestData()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _system.Save(-1, "k", new TestData()));
        }

        [Test]
        public void Load_InvalidSlot_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _system.Load<TestData>(99, "k"));
        }

        // ---------- WriteToDisk ----------

        [Test]
        public void WriteToDisk_CallsStorageWrite()
        {
            _system.Save(0, "key", new TestData { Value = 1 });

            _system.WriteToDisk(0);

            Assert.AreEqual(1, _storage.WriteCalls);
            Assert.AreEqual(1, _serializer.SerializeCalls);
        }

        [Test]
        public void WriteToDisk_WithEncryptor_CallsEncryptThenWrite()
        {
            var sys = new SaveSystem(_serializer, _storage, _encryptor);
            sys.Save(0, "key", new TestData());

            sys.WriteToDisk(0);

            Assert.AreEqual(1, _encryptor.EncryptCalls);
            Assert.AreEqual(1, _storage.WriteCalls);
        }

        [Test]
        public void WriteToDisk_PublishesOnSaveCompleted()
        {
            int? completedSlot = null;
            _system.OnSaveCompleted += slot => completedSlot = slot;

            _system.WriteToDisk(0);

            Assert.AreEqual(0, completedSlot);
        }

        [Test]
        public void WriteToDisk_StorageThrows_PublishesOnSaveFailed()
        {
            LogAssert.Expect(LogType.Error,
                new Regex(@"\[세이브\] 슬롯 0 디스크 저장 실패"));
            _storage.ThrowOnWrite = true;
            int? failedSlot = null;
            Exception failedEx = null;
            _system.OnSaveFailed += (s, e) => { failedSlot = s; failedEx = e; };

            _system.WriteToDisk(0);

            Assert.AreEqual(0, failedSlot);
            Assert.IsNotNull(failedEx);
        }

        // ---------- ReadFromDisk ----------

        [Test]
        public void ReadFromDisk_StorageDoesNotExist_NoOp()
        {
            _system.ReadFromDisk(0);

            Assert.AreEqual(0, _serializer.DeserializeCalls);
            Assert.AreEqual(0, _storage.ReadCalls);
        }

        [Test]
        public void ReadFromDisk_RestoresSlotData()
        {
            var sys1 = new SaveSystem(_serializer, _storage);
            sys1.Save(0, "key", new TestData { Value = 999 });
            sys1.WriteToDisk(0);

            var sys2 = new SaveSystem(_serializer, _storage);
            sys2.ReadFromDisk(0);

            Assert.AreEqual(999, sys2.Load<TestData>(0, "key").Value);
        }

        [Test]
        public void ReadFromDisk_PublishesOnLoadCompleted()
        {
            _system.Save(0, "key", new TestData());
            _system.WriteToDisk(0);

            int? loadedSlot = null;
            _system.OnLoadCompleted += s => loadedSlot = s;
            _system.ReadFromDisk(0);

            Assert.AreEqual(0, loadedSlot);
        }

        [Test]
        public void ReadFromDisk_WithEncryptor_DecryptsBeforeDeserialize()
        {
            var sys = new SaveSystem(_serializer, _storage, _encryptor);
            sys.Save(0, "key", new TestData { Value = 7 });
            sys.WriteToDisk(0);

            sys.ReadFromDisk(0);

            Assert.GreaterOrEqual(_encryptor.DecryptCalls, 1);
            Assert.AreEqual(7, sys.Load<TestData>(0, "key").Value);
        }

        // ---------- DeleteSlot / HasSlot ----------

        [Test]
        public void DeleteSlot_ClearsSlotAndCallsStorageDelete()
        {
            _system.Save(0, "key", new TestData { Value = 1 });
            _system.WriteToDisk(0);

            _system.DeleteSlot(0);

            Assert.AreEqual(1, _storage.DeleteCalls);
            Assert.IsFalse(_system.HasKey(0, "key"));
        }

        [Test]
        public void HasSlot_StorageExistsAfterWrite_ReturnsTrue()
        {
            _system.Save(0, "key", new TestData());
            _system.WriteToDisk(0);

            Assert.IsTrue(_system.HasSlot(0));
        }

        [Test]
        public void HasSlot_EmptyAndNoStorage_ReturnsFalse()
        {
            Assert.IsFalse(_system.HasSlot(2));
        }
    }
}
