using AgencyCampaign.Application.Services;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ContentFileStorage : IContentFileStorage
    {
        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/jpg"] = ".jpg",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

        private readonly IWebHostEnvironment environment;
        private readonly ITenantContext tenantContext;

        public ContentFileStorage(IWebHostEnvironment environment, ITenantContext tenantContext)
        {
            this.environment = environment;
            this.tenantContext = tenantContext;
        }

        public async Task<ContentFileResult> SaveAsync(long deliverableId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            if (!AllowedTypes.TryGetValue(contentType, out string? extension))
            {
                throw new InvalidOperationException("contentReview.upload.unsupportedType");
            }

            string tenantId = tenantContext.HasTenant && !string.IsNullOrWhiteSpace(tenantContext.TenantId)
                ? tenantContext.TenantId.Trim()
                : "default";

            string relativeDir = Path.Combine("uploads", "content", tenantId, deliverableId.ToString());
            string absoluteDir = Path.Combine(environment.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            string storedName = $"{Guid.NewGuid():N}{extension}";
            string absolutePath = Path.Combine(absoluteDir, storedName);
            await using (FileStream target = File.Create(absolutePath))
            {
                await content.CopyToAsync(target, cancellationToken);
            }

            string url = "/" + Path.Combine(relativeDir, storedName).Replace('\\', '/');
            return new ContentFileResult(url, fileName, contentType);
        }

        public void RemoveByVersion(long deliverableId, IEnumerable<string> urls)
        {
            foreach (string url in urls)
            {
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/content/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string absolutePath = Path.Combine(environment.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
        }
    }
}
