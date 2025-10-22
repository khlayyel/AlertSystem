using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AlertSystem.Service
{
    public sealed class ConfirmationTokenService
    {
        private readonly string _secret;

        public ConfirmationTokenService(string secret)
        {
            _secret = string.IsNullOrWhiteSpace(secret) ? "dev-secret-change-me" : secret;
        }

        public string Generate(ConfirmPayload payload)
        {
            payload.Exp = payload.Exp == 0 ? DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds() : payload.Exp;
            var json = JsonSerializer.Serialize(payload);
            var data = Encoding.UTF8.GetBytes(json);
            var sig = Sign(data);
            var packed = new TokenEnvelope { d = Convert.ToBase64String(data), s = Convert.ToBase64String(sig) };
            return Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packed)));
        }

        public bool TryValidate(string token, out ConfirmPayload payload)
        {
            payload = new ConfirmPayload();
            try
            {
                var raw = Encoding.UTF8.GetString(Base64UrlDecode(token));
                var env = JsonSerializer.Deserialize<TokenEnvelope>(raw);
                if (env == null || string.IsNullOrWhiteSpace(env.d) || string.IsNullOrWhiteSpace(env.s)) return false;
                var data = Convert.FromBase64String(env.d);
                var sig = Convert.FromBase64String(env.s);
                if (!Verify(data, sig)) return false;
                payload = JsonSerializer.Deserialize<ConfirmPayload>(Encoding.UTF8.GetString(data)) ?? new ConfirmPayload();
                if (payload.AlerteId == 0 && string.IsNullOrEmpty(payload.Kind) && string.IsNullOrEmpty(payload.Value)) return false;
                if (payload.Exp > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.Exp) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] Sign(byte[] data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            return hmac.ComputeHash(data);
        }

        private bool Verify(byte[] data, byte[] sig)
        {
            var s = Sign(data);
            return CryptographicOperations.FixedTimeEquals(s, sig);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        private sealed class TokenEnvelope
        {
            public string d { get; set; } = string.Empty;
            public string s { get; set; } = string.Empty;
        }
    }

    public sealed class ConfirmPayload
    {
        public int AlerteId { get; set; }
        public string Kind { get; set; } = string.Empty; // "wa" or "email"
        public string Value { get; set; } = string.Empty; // phone or email
        public long Exp { get; set; } // unix seconds
    }
}


