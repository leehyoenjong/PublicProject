using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    [CreateAssetMenu(fileName = "NewEquipmentInfo", menuName = "PublicFramework/Item/EquipmentInfo")]
    public class EquipmentInfo : ScriptableObject, IEquipmentInfo
    {
        [Header("부모 아이템")]
        [SerializeField, SheetAlias("MID")] private int _itemMID;

        [Header("슬롯")]
        [SerializeField, SheetAlias("slotId")] private string _slotId;

        [Header("세트")]
        [SerializeField, SheetAlias("setId")] private int _setId;

        [Header("스킬 (콤마 구분 MID → SO 자동 매칭)")]
        [SerializeField, SheetAlias("skillMIDs")] private SkillData[] _skills;

        [Header("기본 스탯 (ChildTable 주입)")]
        [SerializeField] private PassiveStat[] _baseStats;

        public ItemCategory OwnerCategory => ItemCategory.Equipment;
        public int ItemMID => _itemMID;
        public string SlotId => _slotId;
        public int SetId => _setId;
        public IReadOnlyList<SkillData> Skills => _skills;
        public IReadOnlyList<PassiveStat> BaseStats => _baseStats;
    }
}

