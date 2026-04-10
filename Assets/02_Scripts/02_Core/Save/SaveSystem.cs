using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 세이브/로드 시스템 구현.
    /// 슬롯 기반 저장, IDataSerializer/IDataEncryptor/ISaveStorage로 전략 교체 가능.
    /// </summary>
    public class SaveSystem : ISaveSystem
    {
        private const int MAX_SLOTS = 5;

        private readonly SaveSlot[] _slots;
        private readonly IDataSerializer _serializer;
        private readonly IDataEncryptor _encryptor;
        private readonly ISaveStorage _storage;

        public event Action<int> OnSaveCompleted;
        public event Action<int> OnLoadCompleted;
        public event Action<int, Exception> OnSaveFailed;
        public event Action<int, Exception> OnLoadFailed;

        /// <param name="serializer">직렬화 전략 (필수)</param>
        /// <param name="storage">저장소 전략 (필수)</param>
        /// <param name="encryptor">암호화 전략 (선택, null이면 암호화 안함)</param>
        public SaveSystem(IDataSerializer serializer, ISaveStorage storage, IDataEncryptor encryptor = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _encryptor = encryptor;

            _slots = new SaveSlot[MAX_SLOTS];
            for (int i = 0; i < MAX_SLOTS; i++)
                _slots[i] = new SaveSlot(i);

            Debug.Log("[SaveSystem] Init completed.");
        }

        public void Save<T>(int slotIndex, string key, T data)
        {
            ValidateSlotIndex(slotIndex);
            string json = JsonUtility.ToJson(data);
            _slots[slotIndex].Set(key, json);
        }

        public T Load<T>(int slotIndex, string key)
        {
            ValidateSlotIndex(slotIndex);
            string json = _slots[slotIndex].Get(key);
            if (json == null)
                return default;
            return JsonUtility.FromJson<T>(json);
        }

        public bool HasKey(int slotIndex, string key)
        {
            ValidateSlotIndex(slotIndex);
            return _slots[slotIndex].HasKey(key);
        }

        public void DeleteKey(int slotIndex, string key)
        {
            ValidateSlotIndex(slotIndex);
            _slots[slotIndex].DeleteKey(key);
        }

        public void WriteToDisk(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            try
            {
                var slot = _slots[slotIndex];
                slot.LastSavedAt = DateTime.Now;

                byte[] bytes = _serializer.Serialize(slot);

                if (_encryptor != null)
                    bytes = _encryptor.Encrypt(bytes);

                _storage.Write(slotIndex, bytes);

                Debug.Log($"[SaveSystem] Slot {slotIndex} saved to disk.");
                OnSaveCompleted?.Invoke(slotIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] WriteToDisk failed for slot {slotIndex}: {e}");
                OnSaveFailed?.Invoke(slotIndex, e);
            }
        }

        public void ReadFromDisk(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            if (!_storage.Exists(slotIndex))
            {
                Debug.LogWarning($"[SaveSystem] Slot {slotIndex} does not exist on disk.");
                return;
            }

            try
            {
                byte[] bytes = _storage.Read(slotIndex);
                if (bytes == null)
                    return;

                if (_encryptor != null)
                    bytes = _encryptor.Decrypt(bytes);

                _slots[slotIndex] = _serializer.Deserialize<SaveSlot>(bytes);

                Debug.Log($"[SaveSystem] Slot {slotIndex} loaded from disk.");
                OnLoadCompleted?.Invoke(slotIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ReadFromDisk failed for slot {slotIndex}: {e}");
                OnLoadFailed?.Invoke(slotIndex, e);
            }
        }

        public SaveSlot[] GetAllSlots()
        {
            return _slots;
        }

        public SaveSlot GetSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return _slots[slotIndex];
        }

        public void DeleteSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            _slots[slotIndex].ClearAll();
            _storage.Delete(slotIndex);
            Debug.Log($"[SaveSystem] Slot {slotIndex} deleted.");
        }

        public bool HasSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return !_slots[slotIndex].IsEmpty || _storage.Exists(slotIndex);
        }

        private void ValidateSlotIndex(int index)
        {
            if (index < 0 || index >= MAX_SLOTS)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Slot index must be 0~{MAX_SLOTS - 1}. Got: {index}");
        }
    }
}
