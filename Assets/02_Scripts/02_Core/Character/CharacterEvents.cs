namespace PublicFramework
{
    /// <summary>캐릭터 인스턴스 생성.</summary>
    public struct CharacterCreatedEvent
    {
        public string InstanceId;
        public int ItemMID;
    }

    /// <summary>캐릭터 인스턴스 제거.</summary>
    public struct CharacterRemovedEvent
    {
        public string InstanceId;
        public int ItemMID;
    }

    /// <summary>레벨 변경.</summary>
    public struct CharacterLevelChangedEvent
    {
        public string InstanceId;
        public int OldLevel;
        public int NewLevel;
    }

    /// <summary>각성 단계 변경.</summary>
    public struct CharacterAwakeningChangedEvent
    {
        public string InstanceId;
        public int OldAwakening;
        public int NewAwakening;
    }

    /// <summary>장착 스킬 변경.</summary>
    public struct CharacterSkillEquippedEvent
    {
        public string InstanceId;
        public int Slot;
        public SkillData OldSkill;
        public SkillData NewSkill;
    }
}
