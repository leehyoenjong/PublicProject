namespace PublicFramework
{
    /// <summary>
    /// 뽑기 결과 한 건. 추첨 당첨 아이템과 인벤토리 지급 후 최종 아이템을 모두 담는다.
    /// 중복/치환 판단은 IInventorySystem 책임 — 여기서는 결과만 반영.
    /// </summary>
    public struct GachaRewardItem
    {
        public int OriginalItemMID;
        public int FinalItemMID;
        public int Count;
        public GachaTierRank Tier;
        public bool IsDuplicate;
        public bool WasConverted;
        public bool IsNew;
    }
}
