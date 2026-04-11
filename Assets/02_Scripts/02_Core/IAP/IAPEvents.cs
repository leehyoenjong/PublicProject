namespace PublicFramework
{
    public struct PurchaseRequestEvent
    {
        public string ProductId;
        public IAPProductType ProductType;
    }

    public struct PurchaseCompleteEvent
    {
        public string ProductId;
        public string TransactionId;
        public IAPProductType ProductType;
    }

    public struct PurchaseFailEvent
    {
        public string ProductId;
        public PurchaseFailReason Reason;
    }

    public struct RewardGrantedEvent
    {
        public string ProductId;
        public string RewardId;
        public RewardType RewardType;
        public int Amount;
        public string Source;
    }

    public struct PendingResolvedEvent
    {
        public string ProductId;
        public string TransactionId;
        public bool IsValid;
    }

    public struct SubscriptionStateChangedEvent
    {
        public string ProductId;
        public SubscriptionState OldState;
        public SubscriptionState NewState;
    }
}
