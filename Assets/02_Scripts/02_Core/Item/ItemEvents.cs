namespace PublicFramework
{
    public readonly struct ItemAcquiredEvent
    {
        public readonly int MID;
        public readonly int Count;
        public readonly string InstanceId;
        public readonly object Source;

        public ItemAcquiredEvent(int mid, int count, string instanceId, object source)
        {
            MID = mid;
            Count = count;
            InstanceId = instanceId;
            Source = source;
        }
    }

    public readonly struct ItemConsumedEvent
    {
        public readonly int MID;
        public readonly int Count;
        public readonly string InstanceId;

        public ItemConsumedEvent(int mid, int count, string instanceId)
        {
            MID = mid;
            Count = count;
            InstanceId = instanceId;
        }
    }

    public readonly struct ItemConvertedEvent
    {
        public readonly int OriginalMID;
        public readonly int ConvertedMID;
        public readonly int ConvertedCount;

        public ItemConvertedEvent(int originalMID, int convertedMID, int convertedCount)
        {
            OriginalMID = originalMID;
            ConvertedMID = convertedMID;
            ConvertedCount = convertedCount;
        }
    }

    public readonly struct ItemExpiredEvent
    {
        public readonly int MID;
        public readonly string InstanceId;

        public ItemExpiredEvent(int mid, string instanceId)
        {
            MID = mid;
            InstanceId = instanceId;
        }
    }

    public readonly struct EquipChangedEvent
    {
        public readonly string OwnerId;
        public readonly string SlotId;
        public readonly string OldInstanceId;
        public readonly string NewInstanceId;

        public EquipChangedEvent(string ownerId, string slotId, string oldInstanceId, string newInstanceId)
        {
            OwnerId = ownerId;
            SlotId = slotId;
            OldInstanceId = oldInstanceId;
            NewInstanceId = newInstanceId;
        }
    }
}

