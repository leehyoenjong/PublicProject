using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 검증용: 시작 시 더미 아이템을 인벤토리에 적재한다.
    /// 파생 프로젝트에서는 제거하거나 비활성화한다 (의도적 디버그 도구).
    /// </summary>
    public class DebugItemSeeder : MonoBehaviour
    {
        [System.Serializable]
        public struct Seed
        {
            public int mid;
            public int count;
        }

        [SerializeField] private Seed[] _seeds;

        private void Start()
        {
            StartCoroutine(SeedWhenReady());
        }

        private System.Collections.IEnumerator SeedWhenReady()
        {
            // 부팅 순서 race 회피: InventorySystem 등록까지 최대 2초 대기
            float wait = 0f;
            while (!ServiceLocator.Has<IInventorySystem>() && wait < 2f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            if (!ServiceLocator.Has<IInventorySystem>())
            {
                Debug.LogWarning("[디버그시드] IInventorySystem 미등록 — 더미 적재 생략");
                yield break;
            }

            IInventorySystem inv = ServiceLocator.Get<IInventorySystem>();
            if (_seeds == null) yield break;

            foreach (Seed s in _seeds)
            {
                if (s.count <= 0) continue;
                inv.AddItem(s.mid, s.count, "debug_seed");
            }
            Debug.Log($"[디버그시드] 더미 아이템 {_seeds.Length}종 적재 완료");
        }
    }
}
