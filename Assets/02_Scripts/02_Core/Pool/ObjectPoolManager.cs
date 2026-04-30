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
            Debug.Log("[풀] 초기화 시작.");
        }

        public void CreatePool(GameObject prefab, PoolConfig config)
        {
            if (_pools.ContainsKey(config.Id))
            {
                Debug.LogWarning($"[풀] 풀 '{config.Id}'이(가) 이미 존재함.");
                return;
            }

            Transform poolParent = new GameObject($"Pool_{config.Id}").transform;
            poolParent.SetParent(_rootParent);

            ObjectPool pool = new ObjectPool(prefab, config, poolParent);
            _pools.Add(config.Id, pool);

            Debug.Log($"[풀] 풀 '{config.Id}' 생성됨. 초기: {config.InitialSize}, 최대: {config.MaxSize}");
        }

        public GameObject Spawn(string id, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(id, out ObjectPool pool))
            {
                Debug.LogError($"[풀] 풀 '{id}'을(를) 찾을 수 없음.");
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
                Debug.LogError("[풀] 어떤 풀에도 속하지 않는 오브젝트.");
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
                Debug.LogWarning($"[풀] 풀 '{id}'을(를) 찾을 수 없어 삭제 불가.");
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

            Debug.Log($"[풀] 풀 '{id}' 초기화 및 제거됨.");
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
            _objectToPoolId.Clear();

            Debug.Log("[풀] 모든 풀 초기화됨.");
        }
    }
}
