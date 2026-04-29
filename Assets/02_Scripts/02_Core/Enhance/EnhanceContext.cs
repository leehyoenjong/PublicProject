namespace PublicFramework
{
    /// <summary>
    /// 강화 요청 컨텍스트 구조체.
    /// 보호권/축복/연속시도는 Phase 2-B 정책 — Strategy 가 EnhanceData 의 정책 컬럼과 결합해 적용.
    /// </summary>
    public struct EnhanceContext
    {
        public EnhanceType Type;
        public int TargetSlotIndex;
        public bool UseProtectionTicket;
        public bool UseBlessing;
        public int ConsecutiveAttempts;
    }
}
