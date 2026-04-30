using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Spawn — param1=poolId(IObjectPoolManager 에 등록된 프리팹 ID), param2=onHitSkillId(선택), param3=offset "{x,y,z}" (선택).
    /// 생성된 인스턴스가 IProjectileInit 을 구현하면 Initialize 호출로 caster/level/powerMultiplier 주입.
    /// 프리팹의 Projectile 컴포넌트가 있으면 onHitSkillId 는 별도 API 로 수신 (Projectile.SetOnHit 등 — MVP 에서는 Projectile 이 IProjectileInit 을 직접 구현).
    /// </summary>
    public class SpawnAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.Spawn;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (context.ObjectPool == null)
            {
                Debug.LogError("[스킬액션] IObjectPoolManager가 제공되지 않음.");
                return;
            }

            string poolId = entry.Param1;
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogError("[스킬액션] param1(poolId)가 비어 있음.");
                return;
            }

            Vector3 offset = SkillActionHelpers.ParseVector3(entry.Param3);
            Vector3 spawnPos = context.CasterPosition + offset;

            GameObject instance = context.ObjectPool.Spawn(poolId, spawnPos, Quaternion.identity);
            if (instance == null)
            {
                Debug.LogError($"[스킬액션] 풀 스폰 실패: '{poolId}'");
                return;
            }

            if (instance.TryGetComponent(out IProjectileInit init))
            {
                init.Initialize(context.CasterId, context.Level, context.PowerMultiplier);
            }

            string onHitSkillId = entry.Param2;
            if (!string.IsNullOrEmpty(onHitSkillId))
            {
                var projectile = instance.GetComponent<Projectile>();
                if (projectile != null) projectile.SetOnHitSkillId(onHitSkillId);
            }
        }
    }
}
