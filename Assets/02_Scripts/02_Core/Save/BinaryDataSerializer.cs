using System.IO;
using System.Text;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BinaryWriter/BinaryReader 기반 직렬화 구현.
    /// JsonUtility로 JSON 변환 후 길이 헤더 + UTF8 바이트로 기록.
    /// 향후 필드 단위 바이너리 직렬화로 확장 가능.
    /// </summary>
    public class BinaryDataSerializer : IDataSerializer
    {
        private const int HEADER_VERSION = 1;

        public byte[] Serialize<T>(T data)
        {
            string json = JsonUtility.ToJson(data, false);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write(HEADER_VERSION);
            writer.Write(jsonBytes.Length);
            writer.Write(jsonBytes);

            return stream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            int version = reader.ReadInt32();
            if (version != HEADER_VERSION)
            {
                Debug.LogError($"[BinaryDataSerializer] Unknown version: {version}");
                return default;
            }

            int length = reader.ReadInt32();
            byte[] jsonBytes = reader.ReadBytes(length);
            string json = Encoding.UTF8.GetString(jsonBytes);

            return JsonUtility.FromJson<T>(json);
        }
    }
}
