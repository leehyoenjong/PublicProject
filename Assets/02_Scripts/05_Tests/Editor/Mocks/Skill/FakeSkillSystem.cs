using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 테스트용 ISkillSystem. Cast 호출만 기록.
    /// </summary>
    public class FakeSkillSystem : ISkillSystem
    {
        public struct CastCall
        {
            public string SkillId;
            public string CasterId;
            public string TargetId;
            public int Level;
        }

        private readonly List<CastCall> _calls = new();
        public IReadOnlyList<CastCall> Calls => _calls;

        public bool ReturnSuccess { get; set; } = true;

        public bool Cast(string skillId, string casterId, string targetId, int level = 1)
        {
            _calls.Add(new CastCall { SkillId = skillId, CasterId = casterId, TargetId = targetId, Level = level });
            return ReturnSuccess;
        }

        public bool Cast(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level = 1)
        {
            return Cast(skillId, casterId, targetId, level);
        }

        public void Execute(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level, float powerMultiplier)
        {
            Cast(skillId, casterId, targetId, level);
        }

        public void RegisterSkill(SkillData data) { }
        public SkillData GetSkillData(string skillId) => null;
        public ISkillInstance GetInstance(string casterId, string skillId) => null;
        public IReadOnlyList<ISkillInstance> GetInstances(string casterId) => System.Array.Empty<ISkillInstance>();
        public void Tick(float deltaTime) { }

        public void Clear() => _calls.Clear();
    }
}
