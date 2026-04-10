using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PublicFramework
{
    /// <summary>
    /// AES-256 기반 데이터 암호화/복호화 구현.
    /// </summary>
    public class AesDataEncryptor : IDataEncryptor
    {
        private const int KEY_SIZE = 32;
        private const int IV_SIZE = 16;
        private const int ITERATIONS = 10000;
        private static readonly byte[] DEFAULT_SALT = Encoding.UTF8.GetBytes("PublicFrameworkSalt2024");

        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesDataEncryptor(string password) : this(password, DEFAULT_SALT) { }

        public AesDataEncryptor(string password, byte[] salt)
        {
            using var derive = new Rfc2898DeriveBytes(
                password, salt, ITERATIONS, HashAlgorithmName.SHA256);
            _key = derive.GetBytes(KEY_SIZE);
            _iv = derive.GetBytes(IV_SIZE);
        }

        public byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var memStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }

            return memStream.ToArray();
        }

        public byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var memStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
            }

            return memStream.ToArray();
        }
    }
}
