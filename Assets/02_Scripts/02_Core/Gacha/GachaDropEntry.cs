using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// GachaData 자식 테이블 — 등급 내 아이템 풀. 2단 추첨의 2단(등급 내 아이템 선택).
    /// </summary>
    [System.Serializable]
    public class GachaDropEntry
    {
        [SerializeField, SheetAlias("tier")] private GachaTierRank _tier;
        [SerializeField, SheetAlias("itemMID")] private int _itemMID;
        [SerializeField, SheetAlias("weight")] private int _weight;

        public GachaTierRank Tier => _tier;
        public int ItemMID => _itemMID;
        public int Weight => _weight;
    }
}
