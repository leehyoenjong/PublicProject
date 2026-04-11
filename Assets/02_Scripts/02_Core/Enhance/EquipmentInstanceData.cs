using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 장비 인스턴스 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class EquipmentInstanceData
    {
        [SerializeField] private string _instanceId;
        [SerializeField] private string _equipmentId;
        [SerializeField] private int _level;
        [SerializeField] private int _grade;
        [SerializeField] private int _transcendStep;
        [SerializeField] private int _pityCount;
        [SerializeField] private AwakeningSlotData[] _awakeningSlots;

        public string InstanceId { get => _instanceId; set => _instanceId = value; }
        public string EquipmentId { get => _equipmentId; set => _equipmentId = value; }
        public int Level { get => _level; set => _level = value; }
        public int Grade { get => _grade; set => _grade = value; }
        public int TranscendStep { get => _transcendStep; set => _transcendStep = value; }
        public int PityCount { get => _pityCount; set => _pityCount = value; }
        public AwakeningSlotData[] AwakeningSlots { get => _awakeningSlots; set => _awakeningSlots = value; }
    }
}
