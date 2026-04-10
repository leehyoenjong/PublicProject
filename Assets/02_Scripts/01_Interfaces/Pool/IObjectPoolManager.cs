using UnityEngine;

namespace PublicFramework
{
    public interface IObjectPoolManager : IService
    {
        void CreatePool(GameObject prefab, PoolConfig config);
        GameObject Spawn(string id, Vector3 position, Quaternion rotation);
        void Despawn(GameObject obj);
        void ClearPool(string id);
        void ClearAll();
    }
}
