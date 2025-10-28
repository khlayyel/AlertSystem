using System.Security.Cryptography;
using System.Text;

namespace AlertSystem.Utils.Crypto
{
    /// <summary>
    /// Cryptographic utility functions
    /// </summary>
    public static class CryptoUtils
    {
        /// <summary>
        /// Computes SHA256 hash of the input string
        /// </summary>
        /// <param name="input">String to hash</param>
        /// <returns>SHA256 hash as hexadecimal string</returns>
        public static string ComputeSha256(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Generates a cryptographically secure random string
        /// </summary>
        /// <param name="length">Length of the string to generate</param>
        /// <returns>Random string</returns>
        public static string GenerateRandomString(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            
            var result = new StringBuilder(length);
            foreach (var b in bytes)
                result.Append(chars[b % chars.Length]);
            
            return result.ToString();
        }
    }
}
