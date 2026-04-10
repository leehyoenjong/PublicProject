using System.Text;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// JsonUtility 기반 직렬화 구현.
    /// 개발/디버깅 시 데이터를 읽기 쉬운 JSON으로 저장.
    /// </summary>
    public class JsonDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T data)
        {
            string json = JsonUtility.ToJson(data, false);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
