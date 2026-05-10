using AgencyCampaign.Application.Services;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ImageUploadStorage : IImageUploadStorage
    {
        private const int MaxDimension = 512;
        private const string UploadsRoot = "uploads";

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/webp"
        };

        private readonly IWebHostEnvironment environment;
        private readonly ITenantContext tenantContext;

        public ImageUploadStorage(IWebHostEnvironment environment, ITenantContext tenantContext)
        {
            this.environment = environment;
            this.tenantContext = tenantContext;
        }

        public async Task<string> SaveAsync(string section, long entityId, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            if (!AllowedContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException("Tipo de arquivo nao suportado. Use PNG, JPG ou WEBP.");
            }

            string normalizedSection = NormalizeSection(section);
            string tenantSegment = ResolveTenantSegment();
            string folder = Path.Combine(GetWebRootPath(), UploadsRoot, normalizedSection, tenantSegment);
            Directory.CreateDirectory(folder);

            string fileName = $"{entityId}.webp";
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
            return $"/{UploadsRoot}/{normalizedSection}/{tenantSegment}/{fileName}?v={version}";
        }

        public Task RemoveAsync(string section, long entityId, CancellationToken cancellationToken = default)
        {
            string normalizedSection = NormalizeSection(section);
            string tenantSegment = ResolveTenantSegment();
            string folder = Path.Combine(GetWebRootPath(), UploadsRoot, normalizedSection, tenantSegment);
            string fileName = $"{entityId}.webp";
            string fullPath = Path.Combine(folder, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private static string NormalizeSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
            {
                throw new ArgumentException("Section is required.", nameof(section));
            }

            string trimmed = section.Trim().ToLowerInvariant();
            if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains(".."))
            {
                throw new ArgumentException("Section contains invalid characters.", nameof(section));
            }

            return trimmed;
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
