using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// GachaData 자식 테이블 — 등급 확률표. 2단 추첨의 1단(등급 선택).
    /// </summary>
    [System.Serializable]
    public class GachaTierEntry
    {
        [SerializeField, SheetAlias("tier")] private GachaTierRank _tier;
        [SerializeField, SheetAlias("weight")] private int _weight;

        public GachaTierRank Tier => _tier;
        public int Weight => _weight;
    }
}
