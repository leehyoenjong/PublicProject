using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// DropTableData 에 ChildTable 로 주입되는 단일 드롭 항목.
    /// Weight 는 0~100 의 독립 확률(percent)로 해석된다 (100=항상, 5=5%).
    /// </summary>
    [System.Serializable]
    public class DropEntry : IDropEntry
    {
        [SerializeField, SheetAlias("order")] private int _order;
        [SerializeField, SheetAlias("itemMID")] private int _itemMID;
        [SerializeField, SheetAlias("weight")] private int _weight;
        [SerializeField, SheetAlias("minCount")] private int _minCount = 1;
        [SerializeField, SheetAlias("maxCount")] private int _maxCount = 1;
        [SerializeField, SheetAlias("minPlayerLevel")] private int _minPlayerLevel;
        [SerializeField, SheetAlias("repeatLimit")] private int _repeatLimit;

        public int Order => _order;
        public int ItemMID => _itemMID;
        public int Weight => _weight;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
        public int MinPlayerLevel => _minPlayerLevel;
        public int RepeatLimit => _repeatLimit;
    }
}
