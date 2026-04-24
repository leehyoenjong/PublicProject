using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 상점 상품 정의 계약. ShopData (SO) 가 구현.
    /// 시트 기반 불변 데이터 — 런타임 상태는 IShopProductInstance 로 분리.
    /// </summary>
    public interface IShopProduct
    {
        string MID { get; }
        int DisplayNameKey { get; }
        int DescriptionKey { get; }
        Sprite Icon { get; }

        PaymentType PaymentType { get; }
        string PaymentId { get; }
        int PaymentAmount { get; }

        ResetPeriod ResetPeriod { get; }
        DayOfWeekMask WeeklyMask { get; }
        string EventStartUtc { get; }
        string EventEndUtc { get; }

        int ProductLimit { get; }
        int PlayerLimit { get; }
        LimitScope PlayerLimitScope { get; }

        int DiscountPercent { get; }
        int FirstPurchaseBonusPercent { get; }

        ShopConditionType ConditionType { get; }
        string ConditionValue { get; }

        int SlotIndex { get; }
        bool IsFeatured { get; }
        bool IsActive { get; }

        IReadOnlyList<ShopReward> Rewards { get; }
    }
}
