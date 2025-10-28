using System.Security.Cryptography;
using System.Text;

namespace AlertSystem.WEB.Services
{
    public interface IPasswordService
    {
        string EncryptPassword(string password);
        string DecryptPassword(string encryptedPassword);
        bool VerifyPassword(string providedPassword, string storedEncryptedPassword);
    }

    public class PasswordService : IPasswordService
    {
        private readonly string _key; // 24 chars for 3DES
        private readonly string _iv;  // 8 chars IV

        public PasswordService(IConfiguration configuration)
        {
            // Read from environment or appsettings. Fallback to previous defaults if missing.
            _key = configuration["Password:3DES:Key"] ??
                   Environment.GetEnvironmentVariable("PASSWORD__3DES__KEY") ??
                   "AlertSystem3DESKey123456789012";
            _iv  = configuration["Password:3DES:IV"] ??
                   Environment.GetEnvironmentVariable("PASSWORD__3DES__IV") ??
                   "12345678";
        }

        private static string ComputeMd5Hex(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static byte[] DeriveBytes(string source, int length)
        {
            // Deterministic key/IV derivation from arbitrary text
            using (var sha = SHA256.Create())
            {
                var seed = Encoding.UTF8.GetBytes(source ?? string.Empty);
                var hash = sha.ComputeHash(seed);
                if (length <= hash.Length)
                {
                    return hash.Take(length).ToArray();
                }
                // If more requested, repeat hashing to fill
                var buffer = new List<byte>(hash);
                while (buffer.Count < length)
                {
                    hash = sha.ComputeHash(hash);
                    buffer.AddRange(hash);
                }
                return buffer.Take(length).ToArray();
            }
        }

        public string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (var tripleDES = TripleDES.Create())
            {
                tripleDES.Key = DeriveBytes(_key, 24); // 24 bytes for 3DES
                tripleDES.IV = DeriveBytes(_iv, 8);    // 8 bytes IV
                tripleDES.Mode = CipherMode.CBC;
                tripleDES.Padding = PaddingMode.PKCS7;

                using (var encryptor = tripleDES.CreateEncryptor())
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                using (var tripleDES = TripleDES.Create())
                {
                    tripleDES.Key = DeriveBytes(_key, 24);
                    tripleDES.IV = DeriveBytes(_iv, 8);
                    tripleDES.Mode = CipherMode.CBC;
                    tripleDES.Padding = PaddingMode.PKCS7;

                    using (var decryptor = tripleDES.CreateDecryptor())
                    {
                        var encryptedBytes = Convert.FromBase64String(encryptedPassword);
                        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool VerifyPassword(string providedPassword, string storedEncryptedPassword)
        {
            if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(storedEncryptedPassword))
                return false;

            try
            {
                // 1) Try decrypting stored value with current key/IV
                var decryptedStoredPassword = DecryptPassword(storedEncryptedPassword);
                if (!string.IsNullOrEmpty(decryptedStoredPassword))
                {
                    if (string.Equals(providedPassword, decryptedStoredPassword, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                // 2) If DB still holds plaintext (legacy), accept exact match
                if (string.Equals(providedPassword, storedEncryptedPassword, StringComparison.Ordinal))
                {
                    return true;
                }

                // 3) If DB stores ciphertext compatible with our EncryptPassword, compare encrypted forms
                var reEncrypted = EncryptPassword(providedPassword);
                if (!string.IsNullOrEmpty(reEncrypted) && string.Equals(reEncrypted, storedEncryptedPassword, StringComparison.Ordinal))
                {
                    return true;
                }

                // 4) Legacy MD5(hex) support: if stored value looks like 32 hex chars, compare MD5
                if (storedEncryptedPassword.Length == 32 && storedEncryptedPassword.All(c => Uri.IsHexDigit(c)))
                {
                    var md5 = ComputeMd5Hex(providedPassword);
                    if (string.Equals(md5, storedEncryptedPassword, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
