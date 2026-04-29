using System.Text;
using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IDataSerializer. JsonUtility + UTF8 변환.</summary>
    public class FakeDataSerializer : IDataSerializer
    {
        public int SerializeCalls { get; private set; }
        public int DeserializeCalls { get; private set; }

        public byte[] Serialize<T>(T data)
        {
            SerializeCalls++;
            string json = JsonUtility.ToJson(data);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            DeserializeCalls++;
            if (bytes == null || bytes.Length == 0) return default;
            string json = Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
