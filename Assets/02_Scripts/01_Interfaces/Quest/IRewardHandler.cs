namespace PublicFramework
{
    /// <summary>
    /// 보상 처리 위임 인터페이스. 퀘스트/업적이 공유한다.
    /// </summary>
    public interface IRewardHandler
    {
        void HandleReward(string rewardId, RewardType rewardType, int amount, string source);
    }
}
