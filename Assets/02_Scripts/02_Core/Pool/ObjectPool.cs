using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly PoolConfig _config;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly HashSet<GameObject> _inUse = new HashSet<GameObject>();

        public string Id => _config.Id;
        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;
        public int TotalCount => _available.Count + _inUse.Count;

        public ObjectPool(GameObject prefab, PoolConfig config, Transform parent)
        {
            _prefab = prefab;
            _config = config;
            _parent = parent;

            Prewarm();
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject obj = GetAvailableObject();
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool] Pool '{Id}' exhausted. MaxSize: {_config.MaxSize}");
                return null;
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _inUse.Add(obj);

            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnSpawn();
            }

            Debug.Log($"[ObjectPool] Spawned from '{Id}'. Available: {_available.Count}, InUse: {_inUse.Count}");
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (!_inUse.Remove(obj))
            {
                Debug.LogWarning($"[ObjectPool] Object not managed by pool '{Id}'.");
                return;
            }

            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }

            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Enqueue(obj);

            Debug.Log($"[ObjectPool] Despawned to '{Id}'. Available: {_available.Count}, InUse: {_inUse.Count}");
        }

        public void Clear()
        {
            foreach (GameObject obj in _inUse)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
            _inUse.Clear();

            while (_available.Count > 0)
            {
                GameObject obj = _available.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            Debug.Log($"[ObjectPool] Pool '{Id}' cleared.");
        }

        private void Prewarm()
        {
            for (int i = 0; i < _config.InitialSize; i++)
            {
                GameObject obj = CreateNewObject();
                _available.Enqueue(obj);
            }

            Debug.Log($"[ObjectPool] Pool '{Id}' prewarmed with {_config.InitialSize} objects.");
        }

        private GameObject GetAvailableObject()
        {
            while (_available.Count > 0)
            {
                GameObject obj = _available.Dequeue();
                if (obj != null)
                {
                    return obj;
                }
            }

            if (_config.AutoExpand && TotalCount < _config.MaxSize)
            {
                return CreateNewObject();
            }

            return null;
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            return obj;
        }
    }
}
