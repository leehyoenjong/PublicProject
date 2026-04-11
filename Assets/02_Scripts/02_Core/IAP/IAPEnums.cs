namespace PublicFramework
{
    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription,
        Package
    }

    public enum PurchaseFailReason
    {
        UserCancelled,
        NetworkError,
        ProductNotFound,
        AlreadyOwned,
        PurchaseLimitReached,
        ValidationFailed,
        StoreError
    }

    public enum ReceiptValidationError
    {
        None,
        InvalidReceipt,
        NetworkError,
        ServerError,
        ExpiredReceipt,
        DuplicateTransaction,
        Unknown
    }

    public enum StorePlatform
    {
        GooglePlay,
        AppStore,
        OneStore,
        GalaxyStore,
        Dummy
    }

    public enum SubscriptionPeriod
    {
        Weekly,
        Monthly,
        Yearly
    }

    public enum SubscriptionState
    {
        None,
        Active,
        Expired,
        Cancelled,
        GracePeriod
    }

    public enum RewardType
    {
        Currency,
        Item,
        Premium,
        Ticket,
        Resource
    }

    public enum PackageConditionType
    {
        None,
        LevelRequired,
        FirstPurchase,
        TimeLimited,
        OneTime
    }
}
