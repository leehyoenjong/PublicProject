namespace PublicFramework
{
    /// <summary>
    /// 아이템 스택 처리 방식.
    /// Stack: 같은 MID 수량 누적 (포션, 재료)
    /// Instance: 개체별 고유 InstanceId 발급 + 개별 상태 (강화된 장비)
    /// Convert: 1개 제한, 중복 획득 시 대체 아이템으로 자동 치환 (중복 캐릭터 → 파편)
    /// </summary>
    public enum StackType
    {
        Stack,
        Instance,
        Convert
    }

    /// <summary>
    /// 아이템 대분류. 서브타입 Info SO 의 할당 여부와 대응된다.
    /// Consumable/Material/Ticket/Currency 는 subtypeRef 가 null.
    /// </summary>
    public enum ItemCategory
    {
        Consumable,
        Material,
        Ticket,
        Currency,
        Equipment,
        Character,
        Pet,
        Relic
    }
}

