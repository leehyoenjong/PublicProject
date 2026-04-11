namespace PublicFramework
{
    public enum GachaType
    {
        Normal,
        Pickup,
        Guaranteed,
        Free,
        Selective,
        Custom
    }

    public enum ItemGrade
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum PityType
    {
        HardPity,
        SoftPity,
        PickupGuarantee,
        None
    }

    public enum PityCarryPolicy
    {
        CarryOver,
        Reset,
        Persistent
    }

    public enum DuplicatePolicy
    {
        Allow,
        Convert,
        Reject
    }

    public enum ResetType
    {
        Daily,
        Weekly,
        Manual
    }
}
