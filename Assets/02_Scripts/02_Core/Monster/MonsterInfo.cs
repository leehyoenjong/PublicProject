using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 정의 SO. 캐릭터·펫과 같은 IUnit 을 구현해 스킬·버프·데미지 로직을 공유한다.
    /// 5종 타입의 동작 차이는 onSpawnEvents/onDeathEvents 의 EventId 로 표현 (하드코딩 분기 지양).
    /// </summary>
    [CreateAssetMenu(fileName = "NewMonsterInfo", menuName = "PublicFramework/Monster/MonsterInfo")]
    public class MonsterInfo : ScriptableObject, IMonsterInfo
    {
        [Header("식별")]
        [SerializeField, SheetAlias("MID")] private string _mid;
        [SerializeField, LocalizationKey, SheetAlias("nameKey")] private int _nameKey;
        [SerializeField, LocalizationKey, SheetAlias("descKey")] private int _descKey;
        [SerializeField, SheetAlias("iconAddress")] private string _iconAddress;

        [Header("분류")]
        [SerializeField, SheetAlias("type")] private MonsterType _type;
        [SerializeField, SheetAlias("classTag")] private string _classTag;
        [SerializeField, SheetAlias("elementTag")] private string _elementTag;

        [Header("스탯·스킬")]
        [SerializeField, SheetAlias("baseStatMID")] private string _baseStatMID;
        [SerializeField, SheetAlias("baseSkillMIDs")] private SkillData[] _baseSkills;

        [Header("드롭·AI")]
        [SerializeField, SheetAlias("dropTableMID")] private string _dropTableMID;
        [SerializeField, SheetAlias("aiPresetMID")] private string _aiPresetMID;

        [Header("레벨·보상")]
        [SerializeField, SheetAlias("level")] private int _level = 1;
        [SerializeField, SheetAlias("expReward")] private int _expReward;
        [SerializeField, SheetAlias("goldReward")] private int _goldReward;

        [Header("훅·리액션")]
        [SerializeField, SheetAlias("onSpawnEvents")] private string[] _onSpawnEvents;
        [SerializeField, SheetAlias("onDeathEvents")] private string[] _onDeathEvents;
        [SerializeField, SheetAlias("hitReactionId")] private string _hitReactionId;

        public string MID => _mid;
        public string UnitId => _mid;
        public string BaseStatMID => _baseStatMID;
        public int NameKey => _nameKey;
        public int DescKey => _descKey;
        public string IconAddress => _iconAddress;
        public MonsterType Type => _type;
        public string ClassTag => _classTag;
        public string ElementTag => _elementTag;
        public IReadOnlyList<SkillData> BaseSkills => _baseSkills ?? System.Array.Empty<SkillData>();
        public string DropTableMID => _dropTableMID;
        public string AIPresetMID => _aiPresetMID;
        public int Level => _level;
        public int ExpReward => _expReward;
        public int GoldReward => _goldReward;
        public IReadOnlyList<string> OnSpawnEvents => _onSpawnEvents ?? System.Array.Empty<string>();
        public IReadOnlyList<string> OnDeathEvents => _onDeathEvents ?? System.Array.Empty<string>();
        public string HitReactionId => _hitReactionId;
    }
}
