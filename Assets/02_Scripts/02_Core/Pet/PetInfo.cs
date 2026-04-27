using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 펫 정의 SO. ItemData 하이브리드의 서브타입(IItemSubtypeInfo) + IUnit 동시 구현.
    /// PetRole 은 [Flags] 라 시트 콤마 구분 입력이 EnumConverter 의 Enum.Parse 로 자동 합성된다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPetInfo", menuName = "PublicFramework/Pet/PetInfo")]
    public class PetInfo : ScriptableObject, IPetInfo
    {
        [Header("부모 아이템·식별")]
        [SerializeField, SheetAlias("MID")] private string _mid;
        [SerializeField, SheetAlias("itemMID")] private int _itemMID;
        [SerializeField, LocalizationKey, SheetAlias("nameKey")] private int _nameKey;
        [SerializeField, LocalizationKey, SheetAlias("descKey")] private int _descKey;
        [SerializeField, SheetAlias("iconAddress")] private string _iconAddress;

        [Header("분류")]
        [SerializeField, SheetAlias("roles")] private PetRole _roles;
        [SerializeField, SheetAlias("classTag")] private string _classTag;
        [SerializeField, SheetAlias("elementTag")] private string _elementTag;

        [Header("스탯·스킬")]
        [SerializeField, SheetAlias("baseStatMID")] private string _baseStatMID;
        [SerializeField, SheetAlias("baseSkillMIDs")] private SkillData[] _baseSkills;
        [SerializeField, SheetAlias("skillSlotMax")] private int _skillSlotMax;

        [Header("AI (Phase 1.5 BT 결합용 자리)")]
        [SerializeField, SheetAlias("aiPresetMID")] private string _aiPresetMID;

        [Header("따라다니기 (Phase 2 Mono 사용)")]
        [SerializeField, SheetAlias("followStrategy")] private PetFollowStrategy _followStrategy;
        [SerializeField, SheetAlias("followDistance")] private float _followDistance;
        [SerializeField, SheetAlias("catchUpDistance")] private float _catchUpDistance;
        [SerializeField, SheetAlias("collisionPolicy")] private PetCollisionPolicy _collisionPolicy;

        [Header("훅")]
        [SerializeField, SheetAlias("onAcquireEvents")] private string[] _onAcquireEvents;
        [SerializeField, SheetAlias("onEquipEvents")] private string[] _onEquipEvents;
        [SerializeField, SheetAlias("onUnequipEvents")] private string[] _onUnequipEvents;

        public ItemCategory OwnerCategory => ItemCategory.Pet;
        public string MID => _mid;
        public string UnitId => _mid;
        public string BaseStatMID => _baseStatMID;
        public int ItemMID => _itemMID;
        public int NameKey => _nameKey;
        public int DescKey => _descKey;
        public string IconAddress => _iconAddress;
        public PetRole Roles => _roles;
        public string ClassTag => _classTag;
        public string ElementTag => _elementTag;
        public IReadOnlyList<SkillData> BaseSkills => _baseSkills ?? System.Array.Empty<SkillData>();
        public int SkillSlotMax => _skillSlotMax;
        public string AIPresetMID => _aiPresetMID;
        public PetFollowStrategy FollowStrategy => _followStrategy;
        public float FollowDistance => _followDistance;
        public float CatchUpDistance => _catchUpDistance;
        public PetCollisionPolicy CollisionPolicy => _collisionPolicy;
        public IReadOnlyList<string> OnAcquireEvents => _onAcquireEvents ?? System.Array.Empty<string>();
        public IReadOnlyList<string> OnEquipEvents => _onEquipEvents ?? System.Array.Empty<string>();
        public IReadOnlyList<string> OnUnequipEvents => _onUnequipEvents ?? System.Array.Empty<string>();
    }
}
