namespace PublicFramework
{
    public struct NotificationScheduledEvent
    {
        public string NotificationId;
        public string ChannelId;
        public float DelaySeconds;
    }

    public struct NotificationCancelledEvent
    {
        public string NotificationId;
    }

    public struct NotificationReceivedEvent
    {
        public string NotificationId;
        public NotificationType Type;
        public string Title;
    }

    public struct NotificationOpenedEvent
    {
        public string NotificationId;
        public string DeepLink;
    }

    public struct NotificationPermissionChangedEvent
    {
        public NotificationPermission OldState;
        public NotificationPermission NewState;
    }

    public struct NotificationChannelToggleEvent
    {
        public string ChannelId;
        public bool Enabled;
    }
}
