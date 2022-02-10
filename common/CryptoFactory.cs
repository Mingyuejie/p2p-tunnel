using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class CryptoFactory
    {
        public ICrypto CreateAes(string password)
        {
            return new AesCrypto(password);
        }
        public ICrypto CreateRsa(RsaCrypto.RsaKey key)
        {
            return new RsaCrypto(key);
        }
    }

    public interface ICrypto : IDisposable
    {
        public Memory<byte> Encode(byte[] buffer);
        public ValueTask<Memory<byte>> EncodeAsync(Memory<byte> buffer);
        public Memory<byte> Decode(byte[] buffer);
        public ValueTask<Memory<byte>> DecodeAsync(Memory<byte> buffer);

    }

    public class AesCrypto : ICrypto
    {
        private ICryptoTransform encryptoTransform;
        private ICryptoTransform decryptoTransform;

        public AesCrypto(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                password = GenerateRandomPassword();
            }

            using Aes aes = Aes.Create();
            (aes.Key, aes.IV) = GenerateKeyAndIV(password);
            encryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
            decryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
        }

        public Memory<byte> Encode(byte[] buffer)
        {
            using MemoryStream memory = new MemoryStream();
            using CryptoStream Encryptor = new CryptoStream(memory, encryptoTransform, CryptoStreamMode.Write);

            Encryptor.Write(buffer, 0, buffer.Length);
            Encryptor.FlushFinalBlock();
            return memory.ToArray();
        }
        public async ValueTask<Memory<byte>> EncodeAsync(Memory<byte> buffer)
        {
            using MemoryStream memory = new MemoryStream();
            using CryptoStream Encryptor = new CryptoStream(memory, encryptoTransform, CryptoStreamMode.Write);

            await Encryptor.WriteAsync(buffer);
            Encryptor.FlushFinalBlock();
            return memory.ToArray();
        }

        public Memory<byte> Decode(byte[] buffer)
        {
            using MemoryStream memory = new MemoryStream(buffer);
            using CryptoStream decryptor = new CryptoStream(memory, decryptoTransform, CryptoStreamMode.Read);

            byte[] result = new byte[buffer.Length];
            int length = decryptor.Read(result, 0, result.Length);

            return result.AsMemory(0, length);
        }

        public async ValueTask<Memory<byte>> DecodeAsync(Memory<byte> buffer)
        {
            using MemoryStream memory = new MemoryStream(buffer.ToArray());
            using CryptoStream decryptor = new CryptoStream(memory, decryptoTransform, CryptoStreamMode.Read);

            int length = await decryptor.ReadAsync(buffer);

            return buffer.Slice(0, length);
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

        private string GenerateRandomPassword()
        {
            List<char> password = new List<char>();
            Random random = new Random();

            for (int i = 0; i < 32; i++)
            {
                password.Add((char)random.Next(0, 127));
            }
            return password.ToString();
        }
    }

    public class RsaCrypto : ICrypto
    {
        RsaKey key = new RsaKey();
        RSA rsa = RSA.Create();

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

        public ValueTask<Memory<byte>> DecodeAsync(Memory<byte> buffer)
        {
            return new ValueTask<Memory<byte>>(Decode(buffer.ToArray()));
        }

        public Memory<byte> Encode(byte[] buffer)
        {
            return rsa.Encrypt(buffer, RSAEncryptionPadding.OaepSHA512);
        }

        public ValueTask<Memory<byte>> EncodeAsync(Memory<byte> buffer)
        {
            return new ValueTask<Memory<byte>>(Encode(buffer.ToArray()));
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

        public class RsaKey
        {
            public string PrivateKey { get; set; }
            public string PublicKey { get; set; }
        }
    }
}
