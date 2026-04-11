namespace PublicFramework
{
    public enum MailType
    {
        System,
        Reward,
        Event,
        GM,
        Custom
    }

    public enum MailState
    {
        Unread,
        Read,
        Claimed,
        Expired
    }

    public enum MailDeleteReason
    {
        Manual,
        Expired,
        Overflow
    }
}
