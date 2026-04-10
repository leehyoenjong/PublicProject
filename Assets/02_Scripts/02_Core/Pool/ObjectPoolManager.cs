using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class ObjectPoolManager : IObjectPoolManager
    {
        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private readonly Dictionary<GameObject, string> _objectToPoolId = new Dictionary<GameObject, string>();
        private readonly Transform _rootParent;

        public ObjectPoolManager(Transform rootParent)
        {
            _rootParent = rootParent;
            Debug.Log("[ObjectPoolManager] Init started.");
        }

        public void CreatePool(GameObject prefab, PoolConfig config)
        {
            if (_pools.ContainsKey(config.Id))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{config.Id}' already exists.");
                return;
            }

            Transform poolParent = new GameObject($"Pool_{config.Id}").transform;
            poolParent.SetParent(_rootParent);

            ObjectPool pool = new ObjectPool(prefab, config, poolParent);
            _pools.Add(config.Id, pool);

            Debug.Log($"[ObjectPoolManager] Pool '{config.Id}' created. Initial: {config.InitialSize}, Max: {config.MaxSize}");
        }

        public GameObject Spawn(string id, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(id, out ObjectPool pool))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{id}' not found.");
                return null;
            }

            GameObject obj = pool.Spawn(position, rotation);
            if (obj != null)
            {
                _objectToPoolId[obj] = id;
            }

            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (!_objectToPoolId.TryGetValue(obj, out string id))
            {
                Debug.LogError("[ObjectPoolManager] Object not managed by any pool.");
                return;
            }

            if (_pools.TryGetValue(id, out ObjectPool pool))
            {
                pool.Despawn(obj);
            }

            _objectToPoolId.Remove(obj);
        }

        public void ClearPool(string id)
        {
            if (!_pools.TryGetValue(id, out ObjectPool pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{id}' not found for clearing.");
                return;
            }

            pool.Clear();
            _pools.Remove(id);

            List<GameObject> toRemove = new List<GameObject>();
            foreach (var pair in _objectToPoolId)
            {
                if (pair.Value == id)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (GameObject obj in toRemove)
            {
                _objectToPoolId.Remove(obj);
            }

            Debug.Log($"[ObjectPoolManager] Pool '{id}' cleared and removed.");
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
            _objectToPoolId.Clear();

            Debug.Log("[ObjectPoolManager] All pools cleared.");
        }
    }
}
