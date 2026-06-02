using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Infrastructure.Services
{
    // Armazenamento PRIVADO de midia de revisao de conteudo (NF, pecas, video). Grava fora de
    // wwwroot (nao servido estaticamente) e devolve uma CHAVE; a exibicao e via URL assinada
    // (IMediaAccessTokenService + /api/media). Logos/avatars publicos ficam no ImageUploadStorage.
    public sealed class ContentFileStorage : IContentFileStorage
    {
        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/jpg"] = ".jpg",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
            ["application/pdf"] = ".pdf",
            ["video/mp4"] = ".mp4",
            ["video/quicktime"] = ".mov",
            ["video/webm"] = ".webm"
        };

        private readonly IWebHostEnvironment environment;
        private readonly ITenantContext tenantContext;
        private readonly MediaStorageOptions options;

        public ContentFileStorage(IWebHostEnvironment environment, ITenantContext tenantContext, IOptions<MediaStorageOptions> options)
        {
            this.environment = environment;
            this.tenantContext = tenantContext;
            this.options = options.Value;
        }

        public async Task<ContentFileResult> SaveAsync(long deliverableId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            if (!AllowedTypes.TryGetValue(contentType, out string? extension))
            {
                throw new InvalidOperationException("contentReview.upload.unsupportedType");
            }

            if (content.CanSeek && content.Length > options.MaxUploadBytes)
            {
                throw new InvalidOperationException("contentReview.upload.tooLarge");
            }

            string tenantId = tenantContext.HasTenant && !string.IsNullOrWhiteSpace(tenantContext.TenantId)
                ? tenantContext.TenantId.Trim()
                : "default";

            string storageKey = string.Join('/', "content", tenantId, deliverableId.ToString(), $"{Guid.NewGuid():N}{extension}");
            string root = ResolveRoot();
            string absolutePath = Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            await using (FileStream target = File.Create(absolutePath))
            {
                await content.CopyToAsync(target, cancellationToken);
            }

            return new ContentFileResult(storageKey, fileName, contentType);
        }

        public void RemoveByVersion(long deliverableId, IEnumerable<string> keys)
        {
            string root = Path.GetFullPath(ResolveRoot());

            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string normalized = key.TrimStart('/');
                if (!normalized.StartsWith("content/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string absolutePath = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
                if (!absolutePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    continue;
                }

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
        }

        private string ResolveRoot()
        {
            return string.IsNullOrWhiteSpace(options.PrivateRootPath)
                ? Path.Combine(environment.ContentRootPath, "private-uploads")
                : options.PrivateRootPath;
        }
    }
}
