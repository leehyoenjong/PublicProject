namespace PublicFramework
{
    /// <summary>
    /// 배너 노출 분류. Regular=상시, Event=기간 한정 이벤트, Limited=픽업/콜라보 한정.
    /// UI 탭/정렬용 — 게임 로직에 영향을 주지 않는다.
    /// </summary>
    public enum BannerCategory
    {
        Regular,
        Event,
        Limited
    }

    /// <summary>
    /// 배너 해금 조건. conditionValue 와 짝으로 사용.
    /// Shop 의 ConditionType 과 분리 — 프로젝트별 확장 방향이 달라질 수 있어 독립.
    /// </summary>
    public enum BannerUnlockType
    {
        None,
        MinLevel,
        QuestClear
    }

    /// <summary>
    /// 가챠 등급 공통 enum. GachaTier(등급 확률) + GachaDrop(등급 내 아이템) 양쪽에서 사용.
    /// N=Normal, R=Rare, SR=Super Rare, SSR=Super Super Rare.
    /// </summary>
    public enum GachaTierRank
    {
        N,
        R,
        SR,
        SSR
    }

    /// <summary>
    /// 10연 확정 최소 등급. None=확정 없음.
    /// bonusGuaranteedTier=R 이면 10연 중 최소 1개는 R 이상 보장.
    /// </summary>
    public enum GuaranteedTier
    {
        None,
        N,
        R,
        SR,
        SSR
    }

    /// <summary>
    /// 가챠 서브타입. Phase 1 은 None 만 사용(Normal/Premium/Event/Free 계열).
    /// Phase 2+ 에서 Pickup/StepUp/Box SO 추가 후 subtypeRef 로 연결.
    /// </summary>
    public enum GachaSubtype
    {
        None,
        Pickup,
        StepUp,
        Box
    }
}
