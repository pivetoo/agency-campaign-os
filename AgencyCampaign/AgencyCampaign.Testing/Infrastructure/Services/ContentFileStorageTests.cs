using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Services;
using Archon.Application.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
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
        public async Task SaveAsync_persists_image_and_returns_url()
        {
            ContentFileStorage storage = BuildStorage(out string root);
            using MemoryStream stream = new(new byte[] { 1, 2, 3 });

            ContentFileResult result = await storage.SaveAsync(7, stream, "peca.png", "image/png");

            result.Url.Should().StartWith("/uploads/content/");
            result.ContentType.Should().Be("image/png");
        }

        private static ContentFileStorage BuildStorage(out string root)
        {
            root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Mock<IWebHostEnvironment> env = new();
            env.SetupGet(item => item.WebRootPath).Returns(root);
            Mock<ITenantContext> tenant = new();
            tenant.SetupGet(item => item.TenantId).Returns("tenant-a");
            tenant.SetupGet(item => item.HasTenant).Returns(true);
            return new ContentFileStorage(env.Object, tenant.Object);
        }
    }
}
