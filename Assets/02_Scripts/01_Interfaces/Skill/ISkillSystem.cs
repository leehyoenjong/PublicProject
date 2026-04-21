using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 등록·시전·실행·쿨다운 관리 서비스.
    /// Cast: 외부(플레이어/AI) 에서 스킬 시전 요청. 쿨/코스트 검증 후 액션 시퀀스 실행.
    /// Execute: 내부 스킬 실행(쿨/코스트 스킵). Projectile onHit 등에서 호출.
    /// </summary>
    public interface ISkillSystem : IService
    {
        void RegisterSkill(SkillData data);
        SkillData GetSkillData(string skillId);

        bool Cast(string skillId, string casterId, string targetId, int level = 1);
        bool Cast(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level = 1);
        void Execute(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level, float powerMultiplier);

        ISkillInstance GetInstance(string casterId, string skillId);
        IReadOnlyList<ISkillInstance> GetInstances(string casterId);
        void Tick(float deltaTime);
    }
}
