namespace PublicFramework
{
    /// <summary>몬스터 타입 5종 (고정). 동작 차이는 데이터 기반 훅으로 표현.</summary>
    public enum MonsterType
    {
        Normal,
        Elite,
        Boss,
        Named,
        Event
    }

    /// <summary>몬스터 이벤트 트리거 시점.</summary>
    public enum MonsterEventKind
    {
        Spawn,
        Death
    }
}
