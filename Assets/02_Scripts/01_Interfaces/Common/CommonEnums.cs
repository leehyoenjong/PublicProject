namespace PublicFramework
{
    /// <summary>
    /// 전역 공통 희귀도.
    /// 아이템/캐릭터/펫/유물 등 모든 엔터티에서 공용으로 사용한다.
    /// 프로젝트별로 추가 등급이 필요하면 여기에 확장한다.
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
}

