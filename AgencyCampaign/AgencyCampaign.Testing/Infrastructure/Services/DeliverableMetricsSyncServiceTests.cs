using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class DeliverableMetricsSyncServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IApifySocialMetricsClient> client = null!;
        private DeliverableMetricsSyncService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            client = new Mock<IApifySocialMetricsClient>();
            client.SetupGet(item => item.IsConfigured).Returns(true);
            client.Setup(item => item.FetchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SocialMetricsResult { Likes = 10, Comments = 2, Views = 100, Shares = 1 });
            service = new DeliverableMetricsSyncService(db, client.Object, NullLogger<DeliverableMetricsSyncService>.Instance);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedReferencesAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C", 0m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Creator("Foo").WithId(1));
            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 100m, 10m).WithId(20));
            db.Add(new Platform("IG").WithId(1));
            db.Add(new DeliverableKind("Story").WithId(1));
            await db.SaveChangesAsync();
        }

        private static CampaignDeliverable BuildPublished(long id)
        {
            CampaignDeliverable deliverable = new(10, 20, $"Post {id}", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            deliverable.Publish($"https://x/{id}", null, DateTimeOffset.UtcNow);
            return deliverable.WithId(id);
        }

        [Test]
        public async Task SyncAll_should_skip_deliverables_synced_within_cooldown()
        {
            await SeedReferencesAsync();

            CampaignDeliverable recent = BuildPublished(30);
            recent.RegisterPublicMetrics(5, 1, 50, 0);
            db.Add(recent);
            db.Add(BuildPublished(31));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            int synced = await service.SyncAll(TimeSpan.FromDays(1));

            synced.Should().Be(1);
        }

        [Test]
        public async Task SyncAll_should_sync_all_when_cooldown_is_zero()
        {
            await SeedReferencesAsync();

            CampaignDeliverable recent = BuildPublished(30);
            recent.RegisterPublicMetrics(5, 1, 50, 0);
            db.Add(recent);
            db.Add(BuildPublished(31));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            int synced = await service.SyncAll(TimeSpan.Zero);

            synced.Should().Be(2);
        }

        [Test]
        public async Task SyncAll_should_return_zero_when_client_not_configured()
        {
            client.SetupGet(item => item.IsConfigured).Returns(false);
            await SeedReferencesAsync();
            db.Add(BuildPublished(30));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            int synced = await service.SyncAll(TimeSpan.FromDays(1));

            synced.Should().Be(0);
        }
    }
}
