using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Pool
{
    /// <summary>
    /// EditMode 부분 커버 — Pool 시스템의 ObjectPool.Clear() 가 Object.Destroy 를 호출하므로
    /// ClearPool/ClearAll 흐름은 PlayMode 테스트로 별도 확인 필요.
    /// 본 테스트는 CreatePool / Spawn / Despawn / AutoExpand / MaxSize / IPoolable 콜백만 검증.
    /// </summary>
    public class ObjectPoolManagerTests
    {
        private GameObject _prefab;
        private GameObject _rootGo;
        private ObjectPoolManager _manager;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("PoolPrefab");
            _prefab.SetActive(false);
            _rootGo = new GameObject("PoolRoot");
            _manager = new ObjectPoolManager(_rootGo.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_prefab != null) Object.DestroyImmediate(_prefab);
            if (_rootGo != null) Object.DestroyImmediate(_rootGo);
        }

        private static PoolConfig MakeConfig(string id = "test", int initial = 2, int max = 5, bool autoExpand = true)
        {
            return new PoolConfig(id, initial, max, autoExpand);
        }

        // ---------- CreatePool ----------

        [Test]
        public void CreatePool_New_CreatesPoolParentAndPrewarms()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 3));

            Transform poolParent = _rootGo.transform.Find("Pool_test");
            Assert.IsNotNull(poolParent);
            Assert.AreEqual(3, poolParent.childCount); // prewarmed 3 instances
        }

        [Test]
        public void CreatePool_Duplicate_DoesNotCreateAgain()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 1));
            int before = _rootGo.transform.childCount;

            _manager.CreatePool(_prefab, MakeConfig(initial: 1));

            // 두 번째 호출은 LogWarning + 무시 → root 자식 그대로
            Assert.AreEqual(before, _rootGo.transform.childCount);
        }

        // ---------- Spawn ----------

        [Test]
        public void Spawn_FromPool_ReturnsActiveObject()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 2));

            GameObject obj = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.activeSelf);
        }

        [Test]
        public void Spawn_AppliesPositionAndRotation()
        {
            _manager.CreatePool(_prefab, MakeConfig());
            var pos = new Vector3(1f, 2f, 3f);
            var rot = Quaternion.Euler(0f, 90f, 0f);

            GameObject obj = _manager.Spawn("test", pos, rot);

            Assert.AreEqual(pos, obj.transform.position);
            // Quaternion 직접 비교는 부동소수점 정밀도 차이로 실패할 수 있어 각도 차이로 비교
            Assert.Less(Quaternion.Angle(rot, obj.transform.rotation), 0.001f);
        }

        [Test]
        public void Spawn_UnknownPool_LogsErrorAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, "[ObjectPoolManager] Pool 'nope' not found.");

            GameObject obj = _manager.Spawn("nope", Vector3.zero, Quaternion.identity);

            Assert.IsNull(obj);
        }

        [Test]
        public void Spawn_AutoExpand_CreatesAdditionalWhenAvailable()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 1, max: 3, autoExpand: true));

            // 첫 번째: prewarm 사용
            GameObject a = _manager.Spawn("test", Vector3.zero, Quaternion.identity);
            // 두 번째: AutoExpand 로 새로 생성
            GameObject b = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotSame(a, b);
        }

        [Test]
        public void Spawn_AtMaxSizeNoAutoExpand_ReturnsNull()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 2, max: 2, autoExpand: false));
            _manager.Spawn("test", Vector3.zero, Quaternion.identity);
            _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            // 3번째: 풀 고갈
            GameObject overflow = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            Assert.IsNull(overflow);
        }

        // ---------- Despawn ----------

        [Test]
        public void Despawn_DeactivatesObject()
        {
            _manager.CreatePool(_prefab, MakeConfig());
            GameObject obj = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            _manager.Despawn(obj);

            Assert.IsFalse(obj.activeSelf);
        }

        [Test]
        public void Despawn_ReturnsToPool_ReusableOnNextSpawn()
        {
            _manager.CreatePool(_prefab, MakeConfig(initial: 1, max: 1, autoExpand: false));
            GameObject first = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            _manager.Despawn(first);
            GameObject second = _manager.Spawn("test", Vector3.zero, Quaternion.identity);

            Assert.AreSame(first, second);
        }

        [Test]
        public void Despawn_UnknownObject_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[ObjectPoolManager] Object not managed by any pool.");
            var stranger = new GameObject("Stranger");

            try { _manager.Despawn(stranger); }
            finally { Object.DestroyImmediate(stranger); }
        }

        [Test]
        public void Despawn_RestoresParentToPool()
        {
            _manager.CreatePool(_prefab, MakeConfig());
            GameObject obj = _manager.Spawn("test", Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(null); // 시뮬레이션: spawn 후 부모를 다른 곳으로 이동

            _manager.Despawn(obj);

            Transform poolParent = _rootGo.transform.Find("Pool_test");
            Assert.AreSame(poolParent, obj.transform.parent);
        }

        // IPoolable 콜백 검증은 PlayMode 테스트로 이전 (FakePoolable 이 Editor 어셈블리에 있어
        // AddComponent 가 Editor script 라는 이유로 거부됨). Pool.Clear/ClearAll 도 PlayMode 에서 동시 검증.
    }
}
