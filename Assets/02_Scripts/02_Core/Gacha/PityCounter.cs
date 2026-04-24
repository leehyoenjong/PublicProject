namespace PublicFramework
{
    /// <summary>
    /// IPityCounter 구현 — 가챠별 천장 카운터 런타임 상태.
    /// GachaSystem 이 갱신하고 IGachaRepository 가 영속화.
    /// </summary>
    public class PityCounter : IPityCounter
    {
        private readonly string _gachaMID;

        public string GachaMID => _gachaMID;
        public int PullsSinceLastSSR { get; private set; }
        public int PullsSinceLastPickup { get; private set; }
        public int TotalPullCount { get; private set; }
        public long LastPullAtUtc { get; private set; }

        public PityCounter(string gachaMID)
        {
            _gachaMID = gachaMID;
        }

        public PityCounter(string gachaMID, int pullsSinceSSR, int pullsSincePickup, int total, long lastPullAtUtc)
        {
            _gachaMID = gachaMID;
            PullsSinceLastSSR = pullsSinceSSR;
            PullsSinceLastPickup = pullsSincePickup;
            TotalPullCount = total;
            LastPullAtUtc = lastPullAtUtc;
        }

        /// <summary>추첨 결과 반영. SSR 당첨 시 SSR 카운터 리셋, 픽업 당첨 시 픽업 카운터 리셋.</summary>
        public void ApplyRoll(GachaTierRank tier, bool wasPickup)
        {
            PullsSinceLastSSR = tier == GachaTierRank.SSR ? 0 : PullsSinceLastSSR + 1;
            PullsSinceLastPickup = wasPickup ? 0 : PullsSinceLastPickup + 1;
            TotalPullCount++;
        }

        public void SetLastPullAt(long unixSeconds)
        {
            LastPullAtUtc = unixSeconds;
        }

        /// <summary>카운터 상태를 IDropResolver 가 사용하는 가변 state 로 스냅샷.</summary>
        public PityCounterState ToState()
        {
            return new PityCounterState(PullsSinceLastSSR, PullsSinceLastPickup);
        }

        /// <summary>IDropResolver 가 수정한 state 를 다시 흡수.</summary>
        public void FromState(PityCounterState state)
        {
            PullsSinceLastSSR = state.PullsSinceLastSSR;
            PullsSinceLastPickup = state.PullsSinceLastPickup;
        }
    }
}
