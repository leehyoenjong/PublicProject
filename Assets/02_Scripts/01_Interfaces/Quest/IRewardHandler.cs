namespace PublicFramework
{
    /// <summary>
    /// 보상 처리 위임 인터페이스. 퀘스트/업적이 공유한다.
    /// rewardId 는 ItemData MID(int). 타입 정보는 수신측에서 ItemData 조회로 해결한다.
    /// </summary>
    public interface IRewardHandler
    {
        void HandleReward(int rewardId, int amount, string source);
    }
}
