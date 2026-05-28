using AgencyCampaign.Application.Requests.ContentLicenses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Archon.Core.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ContentLicenseServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private ContentLicenseService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            service = new ContentLicenseService(db, notifications.Object, Options.Create(new ContentLicenseOptions()));
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Add_creates_license_for_deliverable()
        {
            (long _, long deliverableA, long _) = await SeedCampaignWithTwoDeliverables();

            await service.Add(deliverableA, new AddContentLicenseRequest(ContentLicenseType.UgcReuse, "Site", null, DateTimeOffset.UtcNow.AddDays(60), 500m, null, null));

            IReadOnlyList<ContentLicenseModel> result = await service.GetByDeliverable(deliverableA);
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(ContentLicenseType.UgcReuse);
        }

        [Test]
        public async Task ApplyToCampaign_copies_to_other_deliverables_only()
        {
            (long _, long deliverableA, long deliverableB) = await SeedCampaignWithTwoDeliverables();
            ContentLicenseModel license = await service.Add(deliverableA, new AddContentLicenseRequest(ContentLicenseType.PaidWhitelisting, "Ads", null, DateTimeOffset.UtcNow.AddDays(30), null, null, null));

            int applied = await service.ApplyToCampaign(license.Id);

            applied.Should().Be(1);
            (await service.GetByDeliverable(deliverableA)).Should().HaveCount(1);
            (await service.GetByDeliverable(deliverableB)).Should().HaveCount(1);
        }

        [Test]
        public async Task GetExpiring_returns_only_within_window()
        {
            (long _, long deliverableA, long _) = await SeedCampaignWithTwoDeliverables();
            await service.Add(deliverableA, new AddContentLicenseRequest(ContentLicenseType.UgcReuse, null, null, DateTimeOffset.UtcNow.AddDays(5), null, null, null));
            await service.Add(deliverableA, new AddContentLicenseRequest(ContentLicenseType.UgcReuse, null, null, DateTimeOffset.UtcNow.AddDays(90), null, null, null));

            IReadOnlyList<ContentLicenseModel> result = await service.GetExpiring(30);

            result.Should().HaveCount(1);
            result[0].DaysUntilExpiry.Should().Be(5);
        }

        [Test]
        public async Task AlertExpiring_notifies_once_per_threshold()
        {
            (long _, long deliverableA, long _) = await SeedCampaignWithTwoDeliverables();
            await service.Add(deliverableA, new AddContentLicenseRequest(ContentLicenseType.UgcReuse, null, null, DateTimeOffset.UtcNow.AddDays(5), null, null, null));

            int first = await service.AlertExpiring(new[] { 30, 7 });
            int second = await service.AlertExpiring(new[] { 30, 7 });

            first.Should().Be(1);
            second.Should().Be(0);
            notifications.Verify(item => item.Create(It.IsAny<CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task<(long campaignId, long deliverableA, long deliverableB)> SeedCampaignWithTwoDeliverables()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            Campaign campaign = new(brand.Id, "Campanha", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            Creator creator = new("Creator", null, null, null, null);
            db.Add(creator);
            await db.SaveChangesAsync();

            CampaignCreator campaignCreator = new(campaign.Id, creator.Id, 1, 0m, 0m);
            db.Add(campaignCreator);
            await db.SaveChangesAsync();

            Platform platform = new("Instagram");
            db.Add(platform);
            await db.SaveChangesAsync();

            DeliverableKind kind = new("Reels");
            db.Add(kind);
            await db.SaveChangesAsync();

            CampaignDeliverable a = new(campaign.Id, campaignCreator.Id, "Entrega A", kind.Id, platform.Id, DateTimeOffset.UtcNow.AddDays(7), 100m, 20m, 0m);
            db.Add(a);
            CampaignDeliverable b = new(campaign.Id, campaignCreator.Id, "Entrega B", kind.Id, platform.Id, DateTimeOffset.UtcNow.AddDays(7), 100m, 20m, 0m);
            db.Add(b);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            return (campaign.Id, a.Id, b.Id);
        }
    }
}
