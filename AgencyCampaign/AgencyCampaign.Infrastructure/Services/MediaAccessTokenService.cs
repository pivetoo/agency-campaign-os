using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AgencyCampaign.Infrastructure.Services
{
    // URL assinada de midia privada: token = base64url(payload).base64url(hmac), onde
    // payload = "{storageKey}|{expiraEmUnixSeconds}". O HMAC autentica e a expiracao limita a janela.
    public sealed class MediaAccessTokenService : IMediaAccessTokenService
    {
        private readonly MediaStorageOptions options;

        public MediaAccessTokenService(IOptions<MediaStorageOptions> options)
        {
            this.options = options.Value;
        }

        public string BuildSignedUrl(string storageKey, TimeSpan? lifetime = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
            byte[] secret = GetSecretOrThrow();

            TimeSpan window = lifetime ?? TimeSpan.FromMinutes(options.SignedUrlMinutes > 0 ? options.SignedUrlMinutes : 120);
            long expiresAt = DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds();

            string payload = $"{storageKey}|{expiresAt}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] signature = HMACSHA256.HashData(secret, payloadBytes);

            string token = $"{ToBase64Url(payloadBytes)}.{ToBase64Url(signature)}";
            return $"/api/media?t={token}";
        }

        public string ResolveDisplayUrl(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return string.Empty;
            }

            // URL externa (http) ou legada/publica (/uploads/...): devolve como esta.
            if (storedValue.StartsWith("/", StringComparison.Ordinal) || storedValue.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return storedValue;
            }

            try
            {
                return BuildSignedUrl(storedValue);
            }
            catch (InvalidOperationException)
            {
                // Segredo de midia ausente (config): nao derruba a leitura; a midia so nao exibe.
                return storedValue;
            }
        }

        public bool TryReadStorageKey(string token, out string storageKey)
        {
            storageKey = string.Empty;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(options.SigningKey))
            {
                return false;
            }

            string[] parts = token.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            byte[] payloadBytes;
            byte[] providedSignature;
            try
            {
                payloadBytes = FromBase64Url(parts[0]);
                providedSignature = FromBase64Url(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] expectedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SigningKey), payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                return false;
            }

            string payload = Encoding.UTF8.GetString(payloadBytes);
            int separator = payload.LastIndexOf('|');
            if (separator <= 0 || separator == payload.Length - 1)
            {
                return false;
            }

            if (!long.TryParse(payload.AsSpan(separator + 1), out long expiresAt)
                || DateTimeOffset.FromUnixTimeSeconds(expiresAt) <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            storageKey = payload[..separator];
            return !string.IsNullOrWhiteSpace(storageKey);
        }

        private byte[] GetSecretOrThrow()
        {
            if (string.IsNullOrWhiteSpace(options.SigningKey))
            {
                throw new InvalidOperationException("mediaStorage.signingKey.missing");
            }

            return Encoding.UTF8.GetBytes(options.SigningKey);
        }

        private static string ToBase64Url(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] FromBase64Url(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            int remainder = padded.Length % 4;
            if (remainder > 0)
            {
                padded = padded.PadRight(padded.Length + (4 - remainder), '=');
            }

            return Convert.FromBase64String(padded);
        }
    }
}
