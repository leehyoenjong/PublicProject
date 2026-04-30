using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                Debug.LogWarning($"[풀] 풀 '{Id}' 소진됨. 최대 크기: {_config.MaxSize}");
                return null;
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _inUse.Add(obj);

            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnSpawn();
            }

            Debug.Log($"[풀] '{Id}'에서 스폰됨. 대기: {_available.Count}, 사용중: {_inUse.Count}");
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (!_inUse.Remove(obj))
            {
                Debug.LogWarning($"[풀] 풀 '{Id}'에서 관리되지 않는 오브젝트.");
                return;
            }

            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                poolable.OnDespawn();
            }

            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Enqueue(obj);

            Debug.Log($"[풀] '{Id}'으로 반환됨. 대기: {_available.Count}, 사용중: {_inUse.Count}");
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

            Debug.Log($"[풀] 풀 '{Id}' 초기화됨.");
        }

        private void Prewarm()
        {
            for (int i = 0; i < _config.InitialSize; i++)
            {
                GameObject obj = CreateNewObject();
                _available.Enqueue(obj);
            }

            Debug.Log($"[풀] 풀 '{Id}' {_config.InitialSize}개로 예열됨.");
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
            if (_prefab == null)
            {
                Debug.LogError($"[풀] '{_config.Id}' _prefab이 fake-null — Inspector 참조 깨짐 (씬 인스턴스가 아닌 prefab 에셋이어야 함).");
                return null;
            }
#if UNITY_EDITOR
            GameObject obj;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(_prefab))
            {
                // Unity 6 Editor 환경에서 generic Instantiate<GameObject>(prefab asset) 이 InvalidCastException 을,
                // PrefabUtility.InstantiatePrefab 도 fake-null marker 를 반환하는 케이스가 있다. instance 는 정상적으로
                // 씬에 만들어지므로 호출 전후의 scene root 차이로 새 instance 를 직접 찾아낸다.
                Scene scene = _parent.gameObject.scene;
                HashSet<int> beforeRoots = new HashSet<int>();
                foreach (GameObject g in scene.GetRootGameObjects()) beforeRoots.Add(g.GetInstanceID());
                UnityEditor.PrefabUtility.InstantiatePrefab(_prefab);
                obj = null;
                foreach (GameObject g in scene.GetRootGameObjects())
                {
                    if (beforeRoots.Contains(g.GetInstanceID())) continue;
                    obj = g;
                    break;
                }
                if (obj == null)
                {
                    Debug.LogError($"[풀] '{_config.Id}' InstantiatePrefab 후 씬 루트에서 인스턴스를 찾을 수 없음.");
                    return null;
                }
            }
            else
            {
                // prefab asset 이 아닌 scene GameObject (예: EditMode 테스트) — 일반 Instantiate.
                obj = Object.Instantiate(_prefab);
            }
#else
            GameObject obj = Object.Instantiate(_prefab);
#endif
            obj.transform.SetParent(_parent, worldPositionStays: false);
            obj.SetActive(false);
            return obj;
        }
    }
}
