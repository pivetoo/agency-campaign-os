using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Application.MultiTenancy;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AgencyCampaign.Infrastructure.Services
{
    // URL assinada de midia privada: token = base64url(payload).base64url(hmac), onde
    // payload = "{tenant}|{storageKey}|{expiraEmUnixSeconds}". A chave HMAC e derivada POR TENANT
    // (HMAC(segredoGlobal, tenant)), o tenant viaja no payload (auto-descritivo, sem exigir contexto na
    // leitura anonima) e a leitura exige que a storageKey esteja no escopo "content/{tenant}/" - fechando
    // o IDOR entre tenants (uma URL cunhada num tenant nao serve midia de outro).
    public sealed class MediaAccessTokenService : IMediaAccessTokenService
    {
        private readonly MediaStorageOptions options;
        private readonly ITenantContext tenantContext;

        public MediaAccessTokenService(IOptions<MediaStorageOptions> options, ITenantContext tenantContext)
        {
            this.options = options.Value;
            this.tenantContext = tenantContext;
        }

        public string BuildSignedUrl(string storageKey, TimeSpan? lifetime = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
            string tenant = ResolveTenantSegment();
            byte[] secret = GetTenantSecretOrThrow(tenant);

            TimeSpan window = lifetime ?? TimeSpan.FromMinutes(options.SignedUrlMinutes > 0 ? options.SignedUrlMinutes : 120);
            long expiresAt = DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds();

            string payload = $"{tenant}|{storageKey}|{expiresAt}";
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

            string payload = Encoding.UTF8.GetString(payloadBytes);
            int firstSeparator = payload.IndexOf('|');
            int lastSeparator = payload.LastIndexOf('|');
            if (firstSeparator <= 0 || lastSeparator <= firstSeparator || lastSeparator == payload.Length - 1)
            {
                return false;
            }

            string tenant = payload[..firstSeparator];
            string key = payload[(firstSeparator + 1)..lastSeparator];

            // Chave derivada do tenant embutido no proprio payload (que esta autenticado pelo HMAC).
            byte[] expectedSignature = HMACSHA256.HashData(GetTenantSecretOrThrow(tenant), payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                return false;
            }

            if (!long.TryParse(payload.AsSpan(lastSeparator + 1), out long expiresAt)
                || DateTimeOffset.FromUnixTimeSeconds(expiresAt) <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            // Binding de escopo: a chave precisa pertencer ao tenant do token. Bloqueia o uso de um token
            // valido (assinado no proprio tenant) para alcancar midia de outro tenant.
            if (string.IsNullOrWhiteSpace(key) || !key.StartsWith($"content/{tenant}/", StringComparison.Ordinal))
            {
                return false;
            }

            storageKey = key;
            return true;
        }

        // Tenant usado no escopo da midia. Espelha o ContentFileStorage: sem tenant resolvido cai em "default".
        private string ResolveTenantSegment()
        {
            return tenantContext.HasTenant && !string.IsNullOrWhiteSpace(tenantContext.TenantId)
                ? tenantContext.TenantId.Trim()
                : "default";
        }

        private byte[] GetTenantSecretOrThrow(string tenant)
        {
            if (string.IsNullOrWhiteSpace(options.SigningKey))
            {
                throw new InvalidOperationException("mediaStorage.signingKey.missing");
            }

            return HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SigningKey), Encoding.UTF8.GetBytes(tenant));
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
