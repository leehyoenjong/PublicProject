using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 각성 슬롯 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class AwakeningSlotData
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private bool _isUnlocked;
        [SerializeField] private string _optionId;
        [SerializeField] private float _optionValue;
        [SerializeField] private bool _isLocked;

        public int SlotIndex { get => _slotIndex; set => _slotIndex = value; }
        public bool IsUnlocked { get => _isUnlocked; set => _isUnlocked = value; }
        public string OptionId { get => _optionId; set => _optionId = value; }
        public float OptionValue { get => _optionValue; set => _optionValue = value; }
        public bool IsLocked { get => _isLocked; set => _isLocked = value; }
    }
}
