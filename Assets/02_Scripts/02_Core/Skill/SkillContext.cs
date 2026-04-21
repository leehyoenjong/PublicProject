using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 액션 실행 컨텍스트. SkillSystem 이 생성해 각 ISkillAction 에 전달한다.
    /// CasterId/TargetId 는 엔티티 ID (StatSystem/BuffSystem 연동), CasterPos/TargetPos 는 Spawn/Move 용 좌표.
    /// </summary>
    public class SkillContext
    {
        public SkillData SkillData;
        public string CasterId;
        public string TargetId;
        public Vector3 CasterPosition;
        public Vector3 TargetPosition;
        public int Level;
        public float PowerMultiplier;

        public IEventBus EventBus;
        public IBuffSystem BuffSystem;
        public IStatSystem StatSystem;
        public ISoundManager SoundManager;
        public IObjectPoolManager ObjectPool;
        public ISkillSystem SkillSystem;
    }
}
