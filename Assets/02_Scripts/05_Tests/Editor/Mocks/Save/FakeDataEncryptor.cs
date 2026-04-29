namespace PublicFramework.Tests
{
    /// <summary>테스트용 IDataEncryptor. identity 변환 (실제 암호화 없음, 호출 횟수만 기록).</summary>
    public class FakeDataEncryptor : IDataEncryptor
    {
        public int EncryptCalls { get; private set; }
        public int DecryptCalls { get; private set; }

        public byte[] Encrypt(byte[] data) { EncryptCalls++; return data; }
        public byte[] Decrypt(byte[] data) { DecryptCalls++; return data; }
    }
}
