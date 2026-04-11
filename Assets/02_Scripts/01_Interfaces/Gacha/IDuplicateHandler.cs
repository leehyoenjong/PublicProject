namespace PublicFramework
{
    /// <summary>
    /// 중복 보상 처리 인터페이스
    /// </summary>
    public interface IDuplicateHandler
    {
        GachaReward HandleDuplicate(GachaReward reward);
        DuplicateConversion GetConversion(GachaReward reward);
    }

    public struct DuplicateConversion
    {
        public string ConvertedItemId;
        public int ConvertedAmount;
        public ItemGrade OriginalGrade;
    }
}
