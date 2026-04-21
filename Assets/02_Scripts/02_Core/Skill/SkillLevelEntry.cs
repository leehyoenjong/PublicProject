using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 레벨별 오버라이드. SkillLevelTable 시트가 parentId/level 로 SkillData._levelTable 에 주입.
    /// CooldownOverride/CostOverride 가 0 이하이면 SkillData 기본값 사용.
    /// PowerMultiplier 가 0 이면 1.0 기본. DealDamage/Heal 의 amount 파라미터에 곱.
    /// </summary>
    [Serializable]
    public class SkillLevelEntry
    {
        [SerializeField] private int _level;
        [SerializeField] private float _cooldownOverride;
        [SerializeField] private float _costOverride;
        [SerializeField] private float _powerMultiplier;

        public int Level => _level;
        public float CooldownOverride => _cooldownOverride;
        public float CostOverride => _costOverride;
        public float PowerMultiplier => _powerMultiplier <= 0f ? 1f : _powerMultiplier;

        public SkillLevelEntry() { }

        public SkillLevelEntry(int level, float cooldownOverride, float costOverride, float powerMultiplier)
        {
            _level = level;
            _cooldownOverride = cooldownOverride;
            _costOverride = costOverride;
            _powerMultiplier = powerMultiplier;
        }
    }
}
