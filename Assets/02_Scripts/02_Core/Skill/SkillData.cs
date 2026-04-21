using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 정의 ScriptableObject. 시트 SkillData (본체) + SkillAction/SkillLevelTable (자식 테이블) 이 주입.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "PublicFramework/Skill/SkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _skillId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("icon")] private Sprite _icon;

        [Header("분류")]
        [SerializeField] private SkillCategory _category;
        [SerializeField] private string[] _tags;

        [Header("타깃팅")]
        [SerializeField] private SkillTargetType _targetType;
        [SerializeField] private float _range;
        [SerializeField] private float _radius;

        [Header("코스트 / 쿨다운")]
        [SerializeField] private float _cooldown;
        [SerializeField] private SkillCostType _costType;
        [SerializeField] private float _costAmount;

        [Header("레벨")]
        [SerializeField] private int _maxLevel = 1;

        [Header("액션 (자식 테이블 주입)")]
        [SerializeField] private SkillActionEntry[] _actions;

        [Header("레벨 오버라이드 (자식 테이블 주입)")]
        [SerializeField] private SkillLevelEntry[] _levelTable;

        public string SkillId => _skillId;
        public int DisplayName => _displayName;
        public int Description => _description;
        public Sprite Icon => _icon;
        public SkillCategory Category => _category;
        public IReadOnlyList<string> Tags => _tags;
        public SkillTargetType TargetType => _targetType;
        public float Range => _range;
        public float Radius => _radius;
        public float Cooldown => _cooldown;
        public SkillCostType CostType => _costType;
        public float CostAmount => _costAmount;
        public int MaxLevel => _maxLevel <= 0 ? 1 : _maxLevel;
        public IReadOnlyList<SkillActionEntry> Actions => _actions;
        public IReadOnlyList<SkillLevelEntry> LevelTable => _levelTable;

        /// <summary>주어진 레벨에 대한 오버라이드 엔트리. 없으면 null.</summary>
        public SkillLevelEntry GetLevelEntry(int level)
        {
            if (_levelTable == null) return null;
            for (int i = 0; i < _levelTable.Length; i++)
            {
                if (_levelTable[i] != null && _levelTable[i].Level == level) return _levelTable[i];
            }
            return null;
        }
    }
}
