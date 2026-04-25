using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 정의 계약. ItemData 하이브리드의 서브타입 SO 에 구현된다.
    /// ItemMID 는 부모 ItemData, UnitId 는 공통 식별자(ItemMID.ToString()).
    /// </summary>
    public interface ICharacterInfo : IItemSubtypeInfo, IUnit
    {
        int ItemMID { get; }
        CharacterRole Role { get; }
        string ClassTag { get; }
        string ElementTag { get; }
        IReadOnlyList<SkillData> BaseSkills { get; }
        SkillSlotStrategy SlotStrategy { get; }
        int SlotValue { get; }
        int DefaultSkinMID { get; }
        string VoiceSetId { get; }
        string DefaultPositionId { get; }
        IReadOnlyList<CharacterDialogueEntry> Dialogues { get; }
        IReadOnlyList<CharacterProfileEntry> Profiles { get; }
    }
}
