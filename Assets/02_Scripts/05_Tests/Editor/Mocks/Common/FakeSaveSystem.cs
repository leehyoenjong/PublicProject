using System;
using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 테스트용 ISaveSystem. 메모리 기반 key-value 저장만 지원.
    /// </summary>
    public class FakeSaveSystem : ISaveSystem
    {
        private readonly Dictionary<int, Dictionary<string, object>> _slots = new Dictionary<int, Dictionary<string, object>>();

        public event Action<int> OnSaveCompleted;
        public event Action<int> OnLoadCompleted;
        public event Action<int, Exception> OnSaveFailed;
        public event Action<int, Exception> OnLoadFailed;

        public int SaveCallCount { get; private set; }
        public int LoadCallCount { get; private set; }

        public SaveSlot[] GetAllSlots() => Array.Empty<SaveSlot>();
        public SaveSlot GetSlot(int slotIndex) => null;
        public bool HasSlot(int slotIndex) => _slots.ContainsKey(slotIndex);
        public void DeleteSlot(int slotIndex) => _slots.Remove(slotIndex);

        public void Save<T>(int slotIndex, string key, T data)
        {
            if (!_slots.TryGetValue(slotIndex, out var slot))
            {
                slot = new Dictionary<string, object>();
                _slots[slotIndex] = slot;
            }
            slot[key] = data;
            SaveCallCount++;
            OnSaveCompleted?.Invoke(slotIndex);
        }

        public T Load<T>(int slotIndex, string key)
        {
            LoadCallCount++;
            if (_slots.TryGetValue(slotIndex, out var slot) && slot.TryGetValue(key, out object value))
            {
                OnLoadCompleted?.Invoke(slotIndex);
                return (T)value;
            }
            return default;
        }

        public bool HasKey(int slotIndex, string key)
            => _slots.TryGetValue(slotIndex, out var slot) && slot.ContainsKey(key);

        public void DeleteKey(int slotIndex, string key)
        {
            if (_slots.TryGetValue(slotIndex, out var slot)) slot.Remove(key);
        }

        public void WriteToDisk(int slotIndex) { }
        public void ReadFromDisk(int slotIndex) { }

        // Unused event suppression
        private void Suppress()
        {
            OnSaveFailed?.Invoke(0, null);
            OnLoadFailed?.Invoke(0, null);
        }
    }
}
