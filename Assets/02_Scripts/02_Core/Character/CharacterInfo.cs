using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 정의 하이브리드 SO. ItemData 의 subtypeRef 에 할당되어 수집·인벤토리·거래 흐름은 아이템이 처리하고,
    /// 여기선 캐릭터 고유 데이터(역할/스킬슬롯/포지션/대사/프로필) 를 제공한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterInfo", menuName = "PublicFramework/Character/CharacterInfo")]
    public class CharacterInfo : ScriptableObject, ICharacterInfo
    {
        [Header("부모 아이템")]
        [SerializeField, SheetAlias("MID")] private int _itemMID;

        [Header("분류")]
        [SerializeField, SheetAlias("role")] private CharacterRole _role;
        [SerializeField, SheetAlias("classTag")] private string _classTag;
        [SerializeField, SheetAlias("elementTag")] private string _elementTag;

        [Header("스탯")]
        [SerializeField, SheetAlias("baseStatMID")] private string _baseStatMID;
        [SerializeField] private StatDataEntry[] _baseStats;

        [Header("기본 스킬 (콤마 구분 MID → SO 자동 매칭)")]
        [SerializeField, SheetAlias("baseSkillMIDs")] private SkillData[] _baseSkills;

        [Header("스킬 슬롯 계산")]
        [SerializeField, SheetAlias("skillSlotStrategy")] private SkillSlotStrategy _slotStrategy;
        [SerializeField, SheetAlias("skillSlotValue")] private int _slotValue;

        [Header("외형 / 포지션")]
        [SerializeField, SheetAlias("defaultSkinMID")] private int _defaultSkinMID;
        [SerializeField, SheetAlias("voiceSetId")] private string _voiceSetId;
        [SerializeField, SheetAlias("defaultPositionId")] private string _defaultPositionId;

        [Header("대사 (ChildTable 주입)")]
        [SerializeField] private CharacterDialogueEntry[] _dialogues;

        [Header("프로필 (ChildTable 주입)")]
        [SerializeField] private CharacterProfileEntry[] _profiles;

        public ItemCategory OwnerCategory => ItemCategory.Character;
        public int ItemMID => _itemMID;
        public string UnitId => _itemMID.ToString();
        public string BaseStatMID => _baseStatMID;
        public CharacterRole Role => _role;
        public string ClassTag => _classTag;
        public string ElementTag => _elementTag;
        public IReadOnlyList<SkillData> BaseSkills => _baseSkills;
        public SkillSlotStrategy SlotStrategy => _slotStrategy;
        public int SlotValue => _slotValue;
        public int DefaultSkinMID => _defaultSkinMID;
        public string VoiceSetId => _voiceSetId;
        public string DefaultPositionId => _defaultPositionId;
        public IReadOnlyList<CharacterDialogueEntry> Dialogues => _dialogues;
        public IReadOnlyList<CharacterProfileEntry> Profiles => _profiles;
    }
}
