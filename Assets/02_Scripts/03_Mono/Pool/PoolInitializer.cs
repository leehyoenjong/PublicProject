using UnityEngine;

namespace PublicFramework
{
    [System.Serializable]
    public class PoolEntry
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private PoolConfig _config;

        public GameObject Prefab => _prefab;
        public PoolConfig Config => _config;
    }

    public class PoolInitializer : MonoBehaviour
    {
        [SerializeField] private PoolEntry[] _poolEntries;

        private ObjectPoolManager _poolManager;

        public IObjectPoolManager PoolManager => _poolManager;

        private void Awake()
        {
            _poolManager = new ObjectPoolManager(transform);
            ServiceLocator.Register<IObjectPoolManager>(_poolManager);

            if (_poolEntries == null || _poolEntries.Length == 0)
            {
                Debug.LogWarning("[PoolInitializer] No pool entries configured.");
                return;
            }

            foreach (PoolEntry entry in _poolEntries)
            {
                if (entry.Prefab == null)
                {
                    Debug.LogError("[PoolInitializer] Prefab is null. Skipping entry.");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Config.Id))
                {
                    entry.Config.Id = entry.Prefab.name;
                }

                _poolManager.CreatePool(entry.Prefab, entry.Config);
            }

            Debug.Log($"[PoolInitializer] Initialized {_poolEntries.Length} pool(s).");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IObjectPoolManager>();
            _poolManager?.ClearAll();
            Debug.Log("[PoolInitializer] Destroyed. All pools cleared.");
        }
    }
}
