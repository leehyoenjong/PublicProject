namespace PublicFramework.Tests
{
    /// <summary>테스트용 IProbabilityModel. 성공/실패를 사전 지정.</summary>
    public class FakeProbabilityModel : IProbabilityModel
    {
        public bool NextSuccess { get; set; } = true;
        public int RollCallCount { get; private set; }
        public float LastBaseProb { get; private set; }
        public int LastPityCount { get; private set; }
        public int LastMaxPity { get; private set; }

        public bool Roll(float baseProb, int pityCount, int maxPity)
        {
            RollCallCount++;
            LastBaseProb = baseProb;
            LastPityCount = pityCount;
            LastMaxPity = maxPity;
            return NextSuccess;
        }

        public float GetDisplayProb(float baseProb, int pityCount, int maxPity) => baseProb;
    }
}
