using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IPoolable. OnSpawn/OnDespawn 호출 횟수 기록.</summary>
    public class FakePoolable : MonoBehaviour, IPoolable
    {
        public int OnSpawnCalls { get; private set; }
        public int OnDespawnCalls { get; private set; }

        public void OnSpawn() { OnSpawnCalls++; }
        public void OnDespawn() { OnDespawnCalls++; }
    }
}
