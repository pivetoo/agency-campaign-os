using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ContentFileStorageTests
    {
        [Test]
        public async Task SaveAsync_rejects_unsupported_content_type()
        {
            ContentFileStorage storage = BuildStorage(out _);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("x"));

            Func<Task> act = () => storage.SaveAsync(1, stream, "f.txt", "text/plain");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task SaveAsync_persists_into_private_root_and_returns_storage_key()
        {
            ContentFileStorage storage = BuildStorage(out string root);
            using MemoryStream stream = new(new byte[] { 1, 2, 3 });

            ContentFileResult result = await storage.SaveAsync(7, stream, "peca.png", "image/png");

            result.StorageKey.Should().StartWith("content/tenant-a/7/");
            result.StorageKey.Should().NotStartWith("/");
            result.ContentType.Should().Be("image/png");
            File.Exists(Path.Combine(root, result.StorageKey.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
        }

        [Test]
        public async Task SaveAsync_accepts_pdf_and_video()
        {
            ContentFileStorage storage = BuildStorage(out _);
            using MemoryStream pdf = new(new byte[] { 1 });
            using MemoryStream video = new(new byte[] { 1 });

            (await storage.SaveAsync(7, pdf, "nf.pdf", "application/pdf")).StorageKey.Should().EndWith(".pdf");
            (await storage.SaveAsync(7, video, "reel.mp4", "video/mp4")).StorageKey.Should().EndWith(".mp4");
        }

        [Test]
        public async Task SaveAsync_rejects_file_over_size_limit()
        {
            ContentFileStorage storage = BuildStorage(out _, maxBytes: 4);
            using MemoryStream stream = new(new byte[] { 1, 2, 3, 4, 5 });

            Func<Task> act = () => storage.SaveAsync(7, stream, "big.png", "image/png");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task RemoveByVersion_deletes_stored_keys()
        {
            ContentFileStorage storage = BuildStorage(out string root);
            using MemoryStream stream = new(new byte[] { 1, 2, 3 });
            ContentFileResult result = await storage.SaveAsync(7, stream, "peca.png", "image/png");
            string absolute = Path.Combine(root, result.StorageKey.Replace('/', Path.DirectorySeparatorChar));

            storage.RemoveByVersion(7, new[] { result.StorageKey });

            File.Exists(absolute).Should().BeFalse();
        }

        private static ContentFileStorage BuildStorage(out string root, long maxBytes = 26_214_400)
        {
            root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Mock<IWebHostEnvironment> env = new();
            env.SetupGet(item => item.ContentRootPath).Returns(root);
            Mock<ITenantContext> tenant = new();
            tenant.SetupGet(item => item.TenantId).Returns("tenant-a");
            tenant.SetupGet(item => item.HasTenant).Returns(true);
            IOptions<MediaStorageOptions> options = Options.Create(new MediaStorageOptions { PrivateRootPath = root, MaxUploadBytes = maxBytes });
            return new ContentFileStorage(env.Object, tenant.Object, options);
        }
    }
}
