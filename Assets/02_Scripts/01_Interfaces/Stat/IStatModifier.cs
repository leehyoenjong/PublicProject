namespace PublicFramework
{
    /// <summary>
    /// 스탯 가산/배율 수정자. 4단계 계산식의 Flat/Percent/Multiplicative 단계 중 하나에 적용.
    /// 같은 Source 객체로 등록된 Modifier 는 RemoveModifiersFromSource 로 일괄 제거 가능.
    /// CustomKey 가 지정되면 커스텀 스탯에 적용, null/empty 면 TargetStat enum 에 적용.
    /// </summary>
    public interface IStatModifier
    {
        StatType TargetStat { get; }
        string CustomKey { get; }
        StatLayer Layer { get; }
        float Value { get; }
        object Source { get; }
        ModifierSource SourceTag { get; }
        string SourceLabel { get; }
        int Priority { get; }
        bool IsTemporary { get; }
        float RemainingSeconds { get; }
    }
}
