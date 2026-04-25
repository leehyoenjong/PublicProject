namespace PublicFramework
{
    /// <summary>
    /// 스킬 슬롯 개수 계산 전략. 프로젝트별 Custom 전략을 주입하려면 이 인터페이스를 구현하고
    /// CharacterSystem 에 주입한다. 기본 구현은 <see cref="DefaultSkillSlotResolver"/>.
    /// </summary>
    public interface ISkillSlotResolver
    {
        int Resolve(ICharacterInfo info, int level, int awakening, Rarity rarity);
    }
}
