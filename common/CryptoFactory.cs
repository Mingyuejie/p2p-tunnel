using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace common
{
    public interface ICryptoFactory
    {
        /// <summary>
        /// 对称加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public ISymmetricCrypto CreateSymmetric(string password);
        /// <summary>
        /// 非对称加密
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IAsymmetricCrypto CreateAsymmetric(RsaKey key);
    }

    public class CryptoFactory : ICryptoFactory
    {
        public ISymmetricCrypto CreateSymmetric(string password)
        {
            return new AesCrypto(password);
        }
        public IAsymmetricCrypto CreateAsymmetric(RsaKey key)
        {
            return new RsaCrypto(key);
        }
    }

    public interface ICrypto : IDisposable
    {
        public byte[] Encode(byte[] buffer);
        public byte[] Encode(Memory<byte> buffer);
        public Memory<byte> Decode(byte[] buffer);
        public Memory<byte> Decode(Memory<byte> buffer);
    }
    /// <summary>
    /// 非对称加密
    /// </summary>
    public interface IAsymmetricCrypto : ICrypto
    {
        public RsaKey Key { get; }
    }
    /// <summary>
    /// 对称加密
    /// </summary>
    public interface ISymmetricCrypto : ICrypto
    {
        public string Password { get; }
    }

    public class AesCrypto : ISymmetricCrypto
    {
        private ICryptoTransform encryptoTransform;
        private ICryptoTransform decryptoTransform;
        ArrayPool<byte> arrayPool = ArrayPool<byte>.Create();

        public string Password { get; private set; }

        public AesCrypto(string password)
        {
            Password = password;
            if (string.IsNullOrWhiteSpace(Password))
            {
                Password = StringHelper.RandomPasswordStringMd5();
            }


            using Aes aes = Aes.Create();
            (aes.Key, aes.IV) = GenerateKeyAndIV(password);
            encryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
            decryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
        }

        public byte[] Encode(byte[] buffer)
        {
            using MemoryStream memory = new MemoryStream();
            using CryptoStream Encryptor = new CryptoStream(memory, encryptoTransform, CryptoStreamMode.Write);

            Encryptor.Write(buffer, 0, buffer.Length);
            Encryptor.FlushFinalBlock();
            return memory.ToArray();
        }
        public byte[] Encode(Memory<byte> buffer)
        {
            return Encode(buffer.ToArray());
        }

        public Memory<byte> Decode(byte[] buffer)
        {
            using MemoryStream memory = new MemoryStream(buffer);
            using CryptoStream decryptor = new CryptoStream(memory, decryptoTransform, CryptoStreamMode.Read);

            byte[] result = new byte[buffer.Length];
            int length = decryptor.Read(result, 0, result.Length);

            return result.AsMemory(0, length);
        }

        public Memory<byte> Decode(Memory<byte> buffer)
        {
            byte[] bytes = arrayPool.Rent(buffer.Length);
            buffer.CopyTo(bytes.AsMemory());

            Memory<byte> res = Decode(bytes);
            arrayPool.Return(bytes);

            return res;
        }

        public void Dispose()
        {
            encryptoTransform.Dispose();
            encryptoTransform.Dispose();
        }

        private (byte[] Key, byte[] IV) GenerateKeyAndIV(string password)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            byte[] hash = Array.Empty<byte>();

            using (SHA384 sha = SHA384.Create())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(password);
                hash = sha.ComputeHash(buffer);
            }
            Array.Copy(hash, 0, key, 0, 32);
            Array.Copy(hash, 32, iv, 0, 16);
            return (Key: key, IV: iv);
        }
    }

    public class RsaCrypto : IAsymmetricCrypto
    {
        RsaKey key = new RsaKey();
        RSA rsa = RSA.Create();

        public RsaKey Key => key;

        public RsaCrypto()
        {
            CreateKey();
        }
        public RsaCrypto(RsaKey key)
        {
            if (key != null)
            {
                this.key = key;
            }
            else
            {
                CreateKey();
            }
        }

        public Memory<byte> Decode(byte[] buffer)
        {
            return rsa.Decrypt(buffer, RSAEncryptionPadding.OaepSHA512);
        }

        public Memory<byte> Decode(Memory<byte> buffer)
        {
            return Decode(buffer.ToArray());
        }

        public byte[] Encode(byte[] buffer)
        {
            return rsa.Encrypt(buffer, RSAEncryptionPadding.OaepSHA512);
        }

        public byte[] Encode(Memory<byte> buffer)
        {
            return Encode(buffer.ToArray());
        }

        public void Dispose()
        {
            rsa.Dispose();
            key = null;
        }

        private void CreateKey()
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            key.PrivateKey = rsa.ToXmlString(true);
            key.PublicKey = rsa.ToXmlString(false);
        }
       
    }
    public class RsaKey
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }
}
