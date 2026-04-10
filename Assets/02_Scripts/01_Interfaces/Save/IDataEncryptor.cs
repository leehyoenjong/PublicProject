namespace PublicFramework
{
    /// <summary>
    /// 데이터 암호화/복호화 인터페이스.
    /// AES 등 구현체를 교체하여 사용.
    /// </summary>
    public interface IDataEncryptor
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] data);
    }
}
