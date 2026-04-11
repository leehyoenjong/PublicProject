namespace PublicFramework
{
    /// <summary>
    /// 네트워크 직렬화/역직렬화 인터페이스.
    /// </summary>
    public interface INetworkSerializer
    {
        string Serialize<T>(T data);
        T Deserialize<T>(string json);
    }
}
