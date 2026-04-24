using System;

namespace PublicFramework
{
    /// <summary>
    /// 런타임 아이템 인스턴스 계약.
    /// Stack: Count 증감, InstanceId 는 MID 로 대체되거나 고정값 사용.
    /// Instance: 개체별 InstanceId 발급, Count 는 1 고정.
    /// Convert: 중복 획득 시 인벤토리에서 치환되므로 런타임에는 최대 1개만 존재.
    /// </summary>
    public interface IItemInstance
    {
        string InstanceId { get; }
        int MID { get; }
        int Count { get; }
        DateTime AcquiredAt { get; }
        DateTime? ExpireAt { get; }
        bool IsBound { get; }
        bool IsExpired(DateTime now);
    }
}

