using UnityEngine;

namespace PublicFramework
{
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        public virtual void OnSpawn()
        {
            Debug.Log($"[풀가능] '{gameObject.name}' 스폰됨.");
        }

        public virtual void OnDespawn()
        {
            Debug.Log($"[풀가능] '{gameObject.name}' 반환됨.");
        }
    }
}
