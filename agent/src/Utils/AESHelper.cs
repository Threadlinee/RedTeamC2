// AESHelper.cs
// Helper for AES encryption/decryption

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Agent.Utils
{
    public static class AESHelper
    {
        // WARNING: Hardcoded key/IV for demo. Use secure key exchange in production!
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32 bytes for AES-256
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEF0123456789"); // 16 bytes for AES

        public static byte[] Encrypt(byte[] plainBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.Close();
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] cipherBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.Close();
                    return ms.ToArray();
                }
            }
        }

        public static string EncryptString(string plainText)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText)));
        }

        public static string DecryptString(string cipherText)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(cipherText)));
        }
    }
}
