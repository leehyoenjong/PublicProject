namespace PublicFramework
{
    /// <summary>
    /// 스킬 시퀀스 내 단일 액션 블록. SkillActionRegistry 가 SkillActionType → ISkillAction 매핑.
    /// Execute 는 동기적으로 즉시 처리하는 것을 기본으로 하고, 지속 효과는 별도 MonoBehaviour/코루틴에 위임한다.
    /// </summary>
    public interface ISkillAction
    {
        SkillActionType Type { get; }
        void Execute(SkillContext context, SkillActionEntry entry);
    }
}
