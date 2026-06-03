using AgencyCampaign.Application.Services;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    // Download do PDF assinado do provedor com guardas anti-SSRF basicas: apenas HTTPS e limite de
    // tamanho. NOTA: a URL vem do callback do provedor (protegido pelo ProviderCallbackSecret); um
    // allowlist de hosts do provedor e o endurecimento natural (follow-up).
    public sealed class SignedDocumentDownloader : ISignedDocumentDownloader
    {
        private const long MaxBytes = 26_214_400; // 25 MB

        private readonly HttpClient httpClient;
        private readonly ILogger<SignedDocumentDownloader>? logger;

        public SignedDocumentDownloader(HttpClient httpClient, ILogger<SignedDocumentDownloader>? logger = null)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url)
                || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                if (response.Content.Headers.ContentLength is long declared && declared > MaxBytes)
                {
                    return null;
                }

                byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return bytes.Length is 0 or > (int)MaxBytes ? null : bytes;
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Failed to download signed document from provider URL.");
                return null;
            }
        }
    }
}
