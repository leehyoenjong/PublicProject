using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 한 발화자(caster)의 한 스킬 런타임 상태. 쿨다운/레벨만 추적. 실제 액션 실행은 SkillSystem 책임.
    /// </summary>
    public class SkillInstance : ISkillInstance
    {
        private readonly string _skillId;
        private readonly string _casterId;
        private int _level;
        private float _cooldownRemaining;

        public string SkillId => _skillId;
        public string CasterId => _casterId;
        public int Level => _level;
        public float CooldownRemaining => _cooldownRemaining;
        public bool IsReady => _cooldownRemaining <= 0f;

        public SkillInstance(string skillId, string casterId, int level)
        {
            _skillId = skillId;
            _casterId = casterId;
            _level = level <= 0 ? 1 : level;
            _cooldownRemaining = 0f;
        }

        public void SetLevel(int level)
        {
            _level = level <= 0 ? 1 : level;
        }

        public void StartCooldown(float duration)
        {
            _cooldownRemaining = Mathf.Max(0f, duration);
        }

        /// <summary>deltaTime 만큼 쿨다운 감소. 이번 Tick 에서 0 으로 떨어졌는지 반환.</summary>
        public bool TickCooldown(float deltaTime)
        {
            if (_cooldownRemaining <= 0f) return false;
            _cooldownRemaining -= deltaTime;
            if (_cooldownRemaining <= 0f)
            {
                _cooldownRemaining = 0f;
                return true;
            }
            return false;
        }
    }
}
