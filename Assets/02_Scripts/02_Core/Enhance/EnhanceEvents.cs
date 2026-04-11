namespace PublicFramework
{
    public struct EnhanceAttemptEvent
    {
        public string InstanceId;
        public EnhanceType EnhanceType;
        public int BeforeValue;
    }

    public struct EnhanceSuccessEvent
    {
        public string InstanceId;
        public EnhanceType EnhanceType;
        public int BeforeValue;
        public int AfterValue;
    }

    public struct EnhanceFailEvent
    {
        public string InstanceId;
        public EnhanceType EnhanceType;
        public int CurrentPityCount;
        public int MaxPity;
        public EnhanceFailPolicy AppliedPolicy;
    }

    public struct PityReachedEvent
    {
        public string InstanceId;
        public EnhanceType EnhanceType;
    }

    public struct AwakeningCompleteEvent
    {
        public string InstanceId;
        public int SlotIndex;
        public string OptionId;
        public float OptionValue;
    }

    public struct MaterialConsumedEvent
    {
        public EnhanceMaterialType MaterialType;
        public int Amount;
        public string Reason;
    }
}
