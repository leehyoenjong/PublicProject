namespace PublicFramework
{
    /// <summary>
    /// 강화 시도 결과 구조체
    /// </summary>
    public struct EnhanceResult
    {
        public bool IsSuccess;
        public EnhanceType Type;
        public int BeforeValue;
        public int AfterValue;
        public EnhanceFailPolicy FailPolicy;
        public int MaxPity;
    }
}
