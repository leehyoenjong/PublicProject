using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 테스트용 IRewardHandler. 호출 로그 기록.
    /// </summary>
    public class FakeRewardHandler : IRewardHandler
    {
        public struct Call
        {
            public int RewardId;
            public int Amount;
            public string Source;
        }

        private readonly List<Call> _calls = new List<Call>();
        public IReadOnlyList<Call> Calls => _calls;

        public void HandleReward(int rewardId, int amount, string source)
        {
            _calls.Add(new Call { RewardId = rewardId, Amount = amount, Source = source });
        }

        public void Clear() => _calls.Clear();
    }
}
