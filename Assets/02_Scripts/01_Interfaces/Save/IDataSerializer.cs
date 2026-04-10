namespace PublicFramework
{
    /// <summary>
    /// 데이터 직렬화/역직렬화 인터페이스.
    /// JSON, Binary 등 구현체를 교체하여 사용.
    /// </summary>
    public interface IDataSerializer
    {
        byte[] Serialize<T>(T data);
        T Deserialize<T>(byte[] bytes);
    }
}
