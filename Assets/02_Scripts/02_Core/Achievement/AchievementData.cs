using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 업적 정의 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievementData", menuName = "PublicFramework/Achievement/AchievementData")]
    public class AchievementData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _achievementId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private AchievementCategory _category;
        [SerializeField] private bool _isHidden;

        [Header("조건")]
        [SerializeField] private ConditionType _conditionType;
        [SerializeField] private string _conditionTargetId;

        [Header("단계별 데이터")]
        [SerializeField] private AchievementTierData[] _tiers;

        public string AchievementId => _achievementId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public AchievementCategory Category => _category;
        public bool IsHidden => _isHidden;
        public ConditionType ConditionType => _conditionType;
        public string ConditionTargetId => _conditionTargetId;
        public IReadOnlyList<AchievementTierData> Tiers => _tiers;
    }
}
