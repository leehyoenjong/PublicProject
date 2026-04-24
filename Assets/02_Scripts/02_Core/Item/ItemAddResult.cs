using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// InventorySystem.AddItem 호출 결과.
    /// Convert 스택 타입 아이템이 중복 획득되어 치환된 경우 ConvertedItems 에 대체 아이템 목록이 채워진다.
    /// 외부 시스템(가챠/퀘스트 보상 등)은 이 결과를 페이로드에 통합해야 한다.
    /// </summary>
    public readonly struct ItemAddResult
    {
        public readonly bool Success;
        public readonly int RequestedMID;
        public readonly int RequestedCount;
        public readonly int AddedCount;
        public readonly string InstanceId;
        public readonly IReadOnlyList<ConvertedReward> ConvertedItems;

        public ItemAddResult(bool success, int requestedMID, int requestedCount, int addedCount,
            string instanceId, IReadOnlyList<ConvertedReward> convertedItems)
        {
            Success = success;
            RequestedMID = requestedMID;
            RequestedCount = requestedCount;
            AddedCount = addedCount;
            InstanceId = instanceId;
            ConvertedItems = convertedItems;
        }
    }

    /// <summary>
    /// Convert 치환으로 지급된 대체 아이템 1건.
    /// </summary>
    public readonly struct ConvertedReward
    {
        public readonly int MID;
        public readonly int Count;

        public ConvertedReward(int mid, int count)
        {
            MID = mid;
            Count = count;
        }
    }
}

