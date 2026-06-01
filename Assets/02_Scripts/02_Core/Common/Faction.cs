namespace PublicFramework
{
    /// <summary>
    /// 유닛 진영. 타겟팅 · 데미지 · 전투종료 판정의 기준이 되는 가장 기본적인 전투 식별자.
    /// 적대 여부는 FactionRules 로 판정한다(Neutral 은 누구와도 비적대).
    /// </summary>
    public enum Faction
    {
        Friendly,   // 아군 (플레이어 진영: 캐릭터/펫)
        Enemy,      // 적군 (몬스터)
        Neutral     // 중립 (오브젝트/상호작용물 등 — 공격 대상 아님)
    }
}
