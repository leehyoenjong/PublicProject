namespace PublicFramework
{
    public struct MailReceivedEvent
    {
        public string MailId;
        public MailType MailType;
        public string Title;
        public bool HasRewards;
    }

    public struct MailClaimedEvent
    {
        public string MailId;
        public int RewardCount;
    }

    public struct MailClaimAllEvent
    {
        public int ClaimedCount;
        public int TotalRewards;
    }

    public struct MailExpiredEvent
    {
        public string MailId;
        public string Title;
    }

    public struct MailReadEvent
    {
        public string MailId;
    }

    public struct MailDeletedEvent
    {
        public string MailId;
        public MailDeleteReason Reason;
    }

    public struct MailNearFullEvent
    {
        public int CurrentCount;
        public int MaxCount;
    }
}
