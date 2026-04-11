namespace PublicFramework
{
    /// <summary>
    /// Pull 반환 구조체
    /// </summary>
    public struct GachaResult
    {
        public bool Success;
        public GachaReward[] Rewards;
        public PityCounter PityInfo;
        public string FailReason;
    }
}
