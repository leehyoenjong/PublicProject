using System;

namespace PublicFramework
{
    public class ItemInstance : IItemInstance
    {
        public string InstanceId { get; }
        public int MID { get; }
        public int Count { get; internal set; }
        public DateTime AcquiredAt { get; }
        public DateTime? ExpireAt { get; internal set; }
        public bool IsBound { get; internal set; }

        public ItemInstance(string instanceId, int mid, int count, DateTime acquiredAt,
            DateTime? expireAt = null, bool isBound = false)
        {
            InstanceId = instanceId;
            MID = mid;
            Count = count;
            AcquiredAt = acquiredAt;
            ExpireAt = expireAt;
            IsBound = isBound;
        }

        public bool IsExpired(DateTime now)
        {
            return ExpireAt.HasValue && now >= ExpireAt.Value;
        }
    }
}

