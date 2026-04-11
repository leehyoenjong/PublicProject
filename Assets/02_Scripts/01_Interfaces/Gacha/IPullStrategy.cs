namespace PublicFramework
{
    /// <summary>
    /// 가챠 확률 모델 인터페이스
    /// </summary>
    public interface IPullStrategy
    {
        GachaReward[] Pull(DropTable dropTable, PityCounter pityCounter, int count);
        float GetProbability(DropTable dropTable, PityCounter pityCounter, ItemGrade grade);
    }
}
