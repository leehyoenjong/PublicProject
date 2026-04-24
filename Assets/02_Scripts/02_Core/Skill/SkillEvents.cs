namespace PublicFramework
{
    public struct SkillCastStartedEvent
    {
        public string SkillId;
        public string CasterId;
        public string TargetId;
        public int Level;
    }

    public struct SkillCastFailedEvent
    {
        public string SkillId;
        public string CasterId;
        public string Reason;
    }

    public struct SkillCastCompletedEvent
    {
        public string SkillId;
        public string CasterId;
        public string TargetId;
    }

    public struct SkillActionExecutedEvent
    {
        public string SkillId;
        public SkillActionType ActionType;
        public string CasterId;
        public string TargetId;
        public bool Success;
        public string Error;
    }

    public struct SkillCooldownStartedEvent
    {
        public string SkillId;
        public string CasterId;
        public float Duration;
    }

    public struct SkillCooldownEndedEvent
    {
        public string SkillId;
        public string CasterId;
    }

    public struct SkillDamageEvent
    {
        public string SkillId;
        public string CasterId;
        public string TargetId;
        public float Amount;
        public string Element;
    }

    public struct SkillHealEvent
    {
        public string SkillId;
        public string CasterId;
        public string TargetId;
        public float Amount;
    }

    /// <summary>
    /// PlayAnimation 액션이 발행. 캐릭터/몬스터 컨트롤러가 구독해 Animator 재생.
    /// TargetRole=Self → CasterId 의 Animator, Target → TargetId 의 Animator.
    /// Duration 0 이면 자동 재생 길이에 맡김(수동 종료 없음).
    /// </summary>
    public struct SkillAnimationEvent
    {
        public string SkillId;
        public string CasterId;
        public string TargetId;
        public string AnimKey;
        public string TargetRole;
        public int Layer;
        public float Duration;
    }
}
