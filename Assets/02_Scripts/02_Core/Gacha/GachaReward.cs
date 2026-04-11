namespace PublicFramework
{
    /// <summary>
    /// 가챠 보상 데이터
    /// </summary>
    public struct GachaReward
    {
        public string RewardId;
        public string RewardType;
        public ItemGrade Grade;
        public int Amount;
        public bool IsNew;
        public bool IsDuplicate;
    }
}
