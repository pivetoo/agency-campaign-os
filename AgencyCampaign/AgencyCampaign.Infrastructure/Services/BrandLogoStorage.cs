using AgencyCampaign.Application.Services;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BrandLogoStorage : IBrandLogoStorage
    {
        private const int MaxDimension = 512;
        private const string RelativeRoot = "uploads/brands";

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/webp"
        };

        private readonly IWebHostEnvironment environment;
        private readonly ITenantContext tenantContext;

        public BrandLogoStorage(IWebHostEnvironment environment, ITenantContext tenantContext)
        {
            this.environment = environment;
            this.tenantContext = tenantContext;
        }

        public async Task<string> SaveAsync(long brandId, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            if (!AllowedContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException("Tipo de arquivo nao suportado. Use PNG, JPG ou WEBP.");
            }

            string tenantSegment = ResolveTenantSegment();
            string folder = Path.Combine(GetWebRootPath(), RelativeRoot, tenantSegment);
            Directory.CreateDirectory(folder);

            string fileName = $"{brandId}.webp";
            string fullPath = Path.Combine(folder, fileName);

            using Image image = await Image.LoadAsync(content, cancellationToken);
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxDimension, MaxDimension)
            }));

            WebpEncoder encoder = new() { Quality = 85 };
            await image.SaveAsync(fullPath, encoder, cancellationToken);

            string version = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            return $"/{RelativeRoot}/{tenantSegment}/{fileName}?v={version}";
        }

        public Task RemoveAsync(long brandId, string? currentLogoUrl, CancellationToken cancellationToken = default)
        {
            string tenantSegment = ResolveTenantSegment();
            string folder = Path.Combine(GetWebRootPath(), RelativeRoot, tenantSegment);
            string fileName = $"{brandId}.webp";
            string fullPath = Path.Combine(folder, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetWebRootPath()
        {
            string root = string.IsNullOrWhiteSpace(environment.WebRootPath)
                ? Path.Combine(environment.ContentRootPath, "wwwroot")
                : environment.WebRootPath;

            Directory.CreateDirectory(root);
            return root;
        }

        private string ResolveTenantSegment()
        {
            string? tenantId = tenantContext.HasTenant ? tenantContext.TenantId : null;
            return string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();
        }
    }
}
