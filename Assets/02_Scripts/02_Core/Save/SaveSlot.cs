using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 하나의 세이브 슬롯. 키-값 쌍으로 데이터를 저장한다.
    /// </summary>
    [Serializable]
    public class SaveSlot
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private string _slotName;
        [SerializeField] private string _lastSavedAt;
        [SerializeField] private float _playTimeSeconds;
        [SerializeField] private List<string> _keys = new();
        [SerializeField] private List<string> _values = new();

        public int SlotIndex => _slotIndex;
        public string SlotName => _slotName;

        public DateTime LastSavedAt
        {
            get => DateTime.TryParse(_lastSavedAt, out var dt) ? dt : DateTime.MinValue;
            set => _lastSavedAt = value.ToString("O");
        }

        public float PlayTimeSeconds
        {
            get => _playTimeSeconds;
            set => _playTimeSeconds = value;
        }

        public bool IsEmpty => _keys.Count == 0;

        public SaveSlot(int slotIndex)
        {
            _slotIndex = slotIndex;
            _slotName = $"Slot {slotIndex}";
        }

        public void SetName(string name)
        {
            _slotName = name;
        }

        public void Set(string key, string jsonValue)
        {
            int index = _keys.IndexOf(key);
            if (index >= 0)
            {
                _values[index] = jsonValue;
            }
            else
            {
                _keys.Add(key);
                _values.Add(jsonValue);
            }
        }

        public string Get(string key)
        {
            int index = _keys.IndexOf(key);
            if (index >= 0)
                return _values[index];

            Debug.LogWarning($"[SaveSlot] Key '{key}' not found in slot {_slotIndex}.");
            return null;
        }

        public bool HasKey(string key)
        {
            return _keys.Contains(key);
        }

        public void DeleteKey(string key)
        {
            int index = _keys.IndexOf(key);
            if (index >= 0)
            {
                _keys.RemoveAt(index);
                _values.RemoveAt(index);
            }
        }

        public void ClearAll()
        {
            _keys.Clear();
            _values.Clear();
            _lastSavedAt = null;
            _playTimeSeconds = 0f;
        }
    }
}
