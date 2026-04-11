using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// UnityEngine.JsonUtility 기반 직렬화.
    /// </summary>
    public class JsonNetworkSerializer : INetworkSerializer
    {
        public string Serialize<T>(T data)
        {
            return JsonUtility.ToJson(data);
        }

        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
