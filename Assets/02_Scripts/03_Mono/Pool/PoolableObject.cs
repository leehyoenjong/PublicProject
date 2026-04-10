using UnityEngine;

namespace PublicFramework
{
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        public virtual void OnSpawn()
        {
            Debug.Log($"[PoolableObject] '{gameObject.name}' spawned.");
        }

        public virtual void OnDespawn()
        {
            Debug.Log($"[PoolableObject] '{gameObject.name}' despawned.");
        }
    }
}
