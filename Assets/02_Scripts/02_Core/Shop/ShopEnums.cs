using System;

namespace PublicFramework
{
    /// <summary>
    /// 상품 지불 방식. 상품당 1개만 지정.
    /// Ad: 광고 시청, IAP: 인앱 결제, Item: 재화/아이템 차감(교환소 포함).
    /// </summary>
    public enum PaymentType
    {
        Ad,
        IAP,
        Item
    }

    /// <summary>
    /// 상점 갱신 주기. 기준 시각은 UTC 09:00.
    /// None=고정 무제한, EventPeriod=시트 시작/종료시각 사이.
    /// </summary>
    public enum ResetPeriod
    {
        None,
        Daily,
        Weekly,
        Monthly,
        EventPeriod
    }

    /// <summary>
    /// 주간 갱신 시 활성 요일. [Flags] 복수 선택 가능(월+수+금 등).
    /// </summary>
    [Flags]
    public enum DayOfWeekMask
    {
        None = 0,
        Mon = 1 << 0,
        Tue = 1 << 1,
        Wed = 1 << 2,
        Thu = 1 << 3,
        Fri = 1 << 4,
        Sat = 1 << 5,
        Sun = 1 << 6
    }

    /// <summary>
    /// 유저별 구매 제한 적용 범위. Day/Week 는 UTC 09:00 기준 경계.
    /// </summary>
    public enum LimitScope
    {
        Day,
        Week,
        Lifetime
    }

    /// <summary>
    /// 상품 노출 조건 타입. conditionValue 와 짝으로 사용.
    /// 프로젝트별 조건은 이 enum 에 추가해 확장.
    /// </summary>
    public enum ShopConditionType
    {
        None,
        MinLevel,
        QuestClear
    }
}
