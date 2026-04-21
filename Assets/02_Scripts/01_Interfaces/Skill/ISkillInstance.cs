namespace PublicFramework
{
    /// <summary>
    /// 개별 스킬의 런타임 상태. 쿨다운/현재 레벨 추적.
    /// </summary>
    public interface ISkillInstance
    {
        string SkillId { get; }
        int Level { get; }
        float CooldownRemaining { get; }
        bool IsReady { get; }
    }
}
