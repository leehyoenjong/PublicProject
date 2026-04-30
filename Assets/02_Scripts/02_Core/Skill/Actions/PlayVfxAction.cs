using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// PlayVfx — param1=poolId(IObjectPoolManager 에 등록된 VFX 프리팹 ID), entry.Duration 초 뒤 Despawn (0 이하면 자동 반납 안 함).
    /// 생성 위치는 컨텍스트의 TargetPosition. 타깃 없으면 CasterPosition.
    /// </summary>
    public class PlayVfxAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.PlayVfx;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (context.ObjectPool == null)
            {
                Debug.LogError("[스킬액션] IObjectPoolManager가 제공되지 않음.");
                return;
            }

            string poolId = entry.Param1;
            if (string.IsNullOrEmpty(poolId)) return;

            Vector3 pos = string.IsNullOrEmpty(context.TargetId) ? context.CasterPosition : context.TargetPosition;
            GameObject instance = context.ObjectPool.Spawn(poolId, pos, Quaternion.identity);
            if (instance == null)
            {
                Debug.LogError($"[스킬액션] 풀 스폰 실패: '{poolId}'");
                return;
            }

            if (entry.Duration > 0f)
            {
                var auto = instance.GetComponent<AutoDespawner>() ?? instance.AddComponent<AutoDespawner>();
                auto.Setup(context.ObjectPool, entry.Duration);
            }
        }
    }

    /// <summary>지정 시간 후 자기 자신을 ObjectPool 에 반납하는 보조 컴포넌트.</summary>
    public class AutoDespawner : MonoBehaviour
    {
        private IObjectPoolManager _pool;
        private float _remaining;

        public void Setup(IObjectPoolManager pool, float duration)
        {
            _pool = pool;
            _remaining = duration;
            enabled = true;
        }

        private void Update()
        {
            if (_pool == null) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _pool.Despawn(gameObject);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            _pool = null;
        }
    }
}
