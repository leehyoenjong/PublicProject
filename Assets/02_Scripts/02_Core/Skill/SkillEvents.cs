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
}
