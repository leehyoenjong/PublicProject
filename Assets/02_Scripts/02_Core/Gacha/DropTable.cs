using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 드롭 테이블 ScriptableObject. 배너 간 재사용 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDropTable", menuName = "PublicFramework/Gacha/DropTable")]
    public class DropTable : ScriptableObject
    {
        [SerializeField] private string _tableId;
        [SerializeField] private DropEntry[] _entries;

        public string TableId => _tableId;
        public IReadOnlyList<DropEntry> Entries => _entries;

        public int GetTotalWeight()
        {
            int total = 0;
            foreach (DropEntry entry in _entries)
            {
                total += entry.Weight;
            }
            return total;
        }

        public int GetTotalWeightWithPickup()
        {
            int total = 0;
            foreach (DropEntry entry in _entries)
            {
                total += entry.IsPickup ? entry.PickupWeight : entry.Weight;
            }
            return total;
        }
    }

    [Serializable]
    public class DropEntry
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _itemType;
        [SerializeField] private ItemGrade _grade;
        [SerializeField] private int _weight;
        [SerializeField] private bool _isPickup;
        [SerializeField] private int _pickupWeight;

        public string ItemId => _itemId;
        public string ItemType => _itemType;
        public ItemGrade Grade => _grade;
        public int Weight => _weight;
        public bool IsPickup => _isPickup;
        public int PickupWeight => _pickupWeight;
    }
}
