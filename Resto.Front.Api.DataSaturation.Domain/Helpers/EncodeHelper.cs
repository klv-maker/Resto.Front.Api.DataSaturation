using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class EncodeHelper
    {
        private const string encodeSalt = "dsfgdsghweragegsdfags";
        // Generate a random salt (for key derivation) or IV (for cipher mode)
        public static byte[] GenerateRandomBytes(int size)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] buffer = new byte[size];
                rng.GetBytes(buffer);
                return buffer;
            }
        }

        public static string EncryptAndEncode(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            // Use a password-based key derivation function (PBKDF2 is recommended)
            var salt = GenerateRandomBytes(16); // 16 bytes for salt
            var key = new Rfc2898DeriveBytes(encodeSalt, salt, 10000); // 10000 iterations is a good minimum

            using (Aes aes = Aes.Create())
            {
                aes.Key = key.GetBytes(32); // 32 bytes for AES-256 key
                aes.IV = key.GetBytes(16);  // 16 bytes for IV
                aes.Mode = CipherMode.CBC; // Common cipher mode

                // Encrypt the data
                using (var ms = new MemoryStream())
                {
                    // Prepend the salt and IV to the stream for retrieval during decryption
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(aes.IV, 0, aes.IV.Length); // IV also needs to be stored/retrieved

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    // Convert the result (salt + IV + ciphertext) to a Base64 string
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string DecodeAndDecrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            byte[] buffer = Convert.FromBase64String(cipherText);

            // Extract the salt and IV from the beginning of the buffer
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            Buffer.BlockCopy(buffer, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(buffer, salt.Length, iv, 0, iv.Length);

            // Derive the same key using the retrieved salt and password
            var key = new Rfc2898DeriveBytes(encodeSalt, salt, 10000);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key.GetBytes(32);
                aes.IV = iv; // Use the retrieved IV
                aes.Mode = CipherMode.CBC;

                // Decrypt the data (skip the salt and IV bytes)
                using (var ms = new MemoryStream())
                {
                    // Create a MemoryStream with just the ciphertext part
                    var cipherStream = new MemoryStream();
                    cipherStream.Write(buffer, salt.Length + iv.Length, buffer.Length - (salt.Length + iv.Length));
                    cipherStream.Position = 0; // Reset position to read from the start of ciphertext

                    using (var cs = new CryptoStream(cipherStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}
