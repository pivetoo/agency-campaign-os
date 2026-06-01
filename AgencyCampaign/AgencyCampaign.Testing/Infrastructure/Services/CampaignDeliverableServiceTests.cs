using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDeliverables;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using DomainEntities = AgencyCampaign.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignDeliverableServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IFinancialAutoGeneration> financial = null!;
        private CampaignDeliverableService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            financial = new Mock<IFinancialAutoGeneration>();
            service = new CampaignDeliverableService(db, LocalizerMock.Create<AgencyCampaignResource>(), financial.Object, Mock.Of<Archon.Application.Services.INotificationService>(), NullLogger<CampaignDeliverableService>.Instance);
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

        private CreateCampaignDeliverableRequest BuildCreateRequest(DeliverableStatus status = DeliverableStatus.Pending, string? publishedUrl = null)
        {
            return new CreateCampaignDeliverableRequest
            {
                CampaignId = 10,
                CampaignCreatorId = 20,
                Title = "Story 1",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = status,
                PublishedUrl = publishedUrl
            };
        }

        [Test]
        public async Task CreateDeliverable_should_throw_when_references_missing()
        {
            CreateCampaignDeliverableRequest request = BuildCreateRequest();

            Func<Task> act = () => service.CreateDeliverable(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateDeliverable_should_persist_when_references_exist()
        {
            await SeedReferencesAsync();

            CampaignDeliverable result = await service.CreateDeliverable(BuildCreateRequest());

            result.Id.Should().BeGreaterThan(0);
            result.Status.Should().Be(DeliverableStatus.Pending);
            financial.Verify(item => item.GenerateForPublishedDeliverable(It.IsAny<CampaignDeliverable>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateDeliverable_with_published_status_should_require_url()
        {
            await SeedReferencesAsync();

            Func<Task> act = () => service.CreateDeliverable(BuildCreateRequest(DeliverableStatus.Published, publishedUrl: null));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateDeliverable_should_preserve_metrics_not_sent_in_request()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = await service.CreateDeliverable(BuildCreateRequest());

            CampaignDeliverable tracked = await db.Set<CampaignDeliverable>().AsTracking().FirstAsync(item => item.Id == deliverable.Id);
            tracked.RegisterCreatorInsights(5000, 4000, 30);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "Story 1",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = DeliverableStatus.Pending,
                Likes = 500
            };
            await service.UpdateDeliverable(deliverable.Id, request);

            db.ChangeTracker.Clear();
            CampaignDeliverable updated = await db.Set<CampaignDeliverable>().AsNoTracking().FirstAsync(item => item.Id == deliverable.Id);
            updated.Likes.Should().Be(500);
            updated.Reach.Should().Be(5000);
            updated.Impressions.Should().Be(4000);
            updated.Saves.Should().Be(30);
        }

        [Test]
        public async Task CreateDeliverable_with_published_status_should_require_brand_approval()
        {
            await SeedReferencesAsync();

            Func<Task> act = () => service.CreateDeliverable(BuildCreateRequest(DeliverableStatus.Published, "https://x"));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateDeliverable_promoting_to_published_should_trigger_payout_once()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();

            DeliverableApproval brand = new(deliverable.Id, DeliverableApprovalType.Brand, "Brand");
            brand.Approve();
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "x",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = DeliverableStatus.Published,
                PublishedUrl = "https://x"
            };

            CampaignDeliverable result = await service.UpdateDeliverable(deliverable.Id, request);

            result.Status.Should().Be(DeliverableStatus.Published);
            financial.Verify(item => item.GenerateForPublishedDeliverable(It.IsAny<CampaignDeliverable>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateDeliverable_should_throw_when_id_mismatch()
        {
            UpdateCampaignDeliverableRequest request = new() { Id = 5, Title = "x", DeliverableKindId = 1, PlatformId = 1, DueAt = DateTimeOffset.UtcNow };

            Func<Task> act = () => service.UpdateDeliverable(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateDeliverable_should_throw_when_not_found()
        {
            UpdateCampaignDeliverableRequest request = new() { Id = 1, Title = "x", DeliverableKindId = 1, PlatformId = 1, DueAt = DateTimeOffset.UtcNow };

            Func<Task> act = () => service.UpdateDeliverable(1, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetDeliverableById_should_return_null_when_not_found()
        {
            (await service.GetDeliverableById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetDeliverableById_should_return_deliverable_when_found()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();

            CampaignDeliverable? result = await service.GetDeliverableById(deliverable.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetDeliverables_should_return_paged_result()
        {
            await SeedReferencesAsync();
            db.Add(new CampaignDeliverable(10, 20, "a", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 100m, 50m, 0m));
            db.Add(new CampaignDeliverable(10, 20, "b", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 100m, 50m, 0m));
            await db.SaveChangesAsync();

            var result = await service.GetDeliverables(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task UpdateDeliverable_should_persist_changes()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "old", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "new",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(10),
                GrossAmount = 2000m,
                CreatorAmount = 1500m,
                AgencyFeeAmount = 200m,
                Status = DeliverableStatus.Pending
            };

            CampaignDeliverable result = await service.UpdateDeliverable(deliverable.Id, request);

            result.Title.Should().Be("new");
            result.GrossAmount.Should().Be(2000m);
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_and_order_by_due_date()
        {
            await SeedReferencesAsync();

            db.Add(new CampaignDeliverable(10, 20, "C-far", 1, 1, DateTimeOffset.UtcNow.AddDays(10), 100m, 50m, 0m).WithId(1));
            db.Add(new CampaignDeliverable(10, 20, "C-soon", 1, 1, DateTimeOffset.UtcNow.AddDays(1), 100m, 50m, 0m).WithId(2));
            db.Add(new CampaignDeliverable(99, 20, "outro", 1, 1, DateTimeOffset.UtcNow, 100m, 50m, 0m).WithId(3));
            await db.SaveChangesAsync();

            List<CampaignDeliverable> result = await service.GetByCampaign(10);

            result.Select(item => item.Title).Should().Equal("C-soon", "C-far");
        }

        [Test]
        public async Task UpdateDeliverable_promoting_to_published_should_notify_when_payout_generation_fails()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();

            DeliverableApproval brand = new(deliverable.Id, DeliverableApprovalType.Brand, "Brand");
            brand.Approve();
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Mock<Archon.Application.Services.INotificationService> notification = new();
            financial.Setup(item => item.GenerateForPublishedDeliverable(It.IsAny<CampaignDeliverable>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("payout error"));
            service = new CampaignDeliverableService(db, LocalizerMock.Create<AgencyCampaignResource>(), financial.Object, notification.Object, NullLogger<CampaignDeliverableService>.Instance);

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "x",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = DeliverableStatus.Published,
                PublishedUrl = "https://x"
            };

            CampaignDeliverable result = await service.UpdateDeliverable(deliverable.Id, request);

            result.Status.Should().Be(DeliverableStatus.Published);
            notification.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateDeliverable_should_swallow_notification_exception_when_payout_fails()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();

            DeliverableApproval brand = new(deliverable.Id, DeliverableApprovalType.Brand, "Brand");
            brand.Approve();
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Mock<Archon.Application.Services.INotificationService> notification = new();
            notification.Setup(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("notification failed"));
            financial.Setup(item => item.GenerateForPublishedDeliverable(It.IsAny<CampaignDeliverable>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("payout error"));
            service = new CampaignDeliverableService(db, LocalizerMock.Create<AgencyCampaignResource>(), financial.Object, notification.Object, NullLogger<CampaignDeliverableService>.Instance);

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "x",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = DeliverableStatus.Published,
                PublishedUrl = "https://x"
            };

            Func<Task> act = () => service.UpdateDeliverable(deliverable.Id, request);

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task CreateDeliverable_with_published_should_trigger_payout_when_brand_approval_exists()
        {
            await SeedReferencesAsync();
            CampaignDeliverable existing = new(10, 20, "stub", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 100m, 80m, 10m);
            db.Add(existing);
            await db.SaveChangesAsync();
            DeliverableApproval brand = new(existing.Id, DeliverableApprovalType.Brand, "Brand");
            brand.Approve();
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreateCampaignDeliverableRequest request = BuildCreateRequest(DeliverableStatus.Pending);

            CampaignDeliverable created = await service.CreateDeliverable(request);

            created.Status.Should().Be(DeliverableStatus.Pending);
        }

        [Test]
        public async Task CreateDeliverable_should_throw_when_campaign_creator_belongs_to_different_campaign()
        {
            await SeedReferencesAsync();
            db.Add(new Campaign(1, "Outra", 0m, DateTimeOffset.UtcNow).WithId(99));
            db.Add(new DomainEntities.CampaignCreator(99, 1, 1, 100m, 10m).WithId(999));
            await db.SaveChangesAsync();

            CreateCampaignDeliverableRequest request = BuildCreateRequest();
            request.CampaignCreatorId = 999;

            Func<Task> act = () => service.CreateDeliverable(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateDeliverable_should_throw_when_kind_inactive()
        {
            await SeedReferencesAsync();
            DeliverableKind inactiveKind = new DeliverableKind("Inactive").WithId(2);
            inactiveKind.Update("Inactive", 0, isActive: false);
            db.Add(inactiveKind);
            await db.SaveChangesAsync();

            CreateCampaignDeliverableRequest request = BuildCreateRequest();
            request.DeliverableKindId = 2;

            Func<Task> act = () => service.CreateDeliverable(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateDeliverable_should_throw_when_platform_inactive()
        {
            await SeedReferencesAsync();
            Platform inactivePlatform = new("Old");
            typeof(Platform).GetProperty("IsActive")!.SetValue(inactivePlatform, false);
            db.Add(inactivePlatform.WithId(2));
            await db.SaveChangesAsync();

            CreateCampaignDeliverableRequest request = BuildCreateRequest();
            request.PlatformId = 2;

            Func<Task> act = () => service.CreateDeliverable(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_return_null_when_not_found()
        {
            CampaignDeliverable? result = await service.Delete(99);
            result.Should().BeNull();
        }

        [Test]
        public async Task Delete_should_remove_deliverable_when_found()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 100m, 50m, 0m);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            long id = deliverable.Id;
            db.ChangeTracker.Clear();

            CampaignDeliverable? result = await service.Delete(id);

            result.Should().NotBeNull();
            (await db.Set<CampaignDeliverable>().AsNoTracking().AnyAsync(item => item.Id == id)).Should().BeFalse();
        }

        [Test]
        public async Task UpdateDeliverable_should_update_evidence_url_when_not_publishing()
        {
            await SeedReferencesAsync();
            CampaignDeliverable deliverable = new(10, 20, "x", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDeliverableRequest request = new()
            {
                Id = deliverable.Id,
                Title = "x",
                DeliverableKindId = 1,
                PlatformId = 1,
                DueAt = DateTimeOffset.UtcNow.AddDays(5),
                GrossAmount = 1000m,
                CreatorAmount = 800m,
                AgencyFeeAmount = 100m,
                Status = DeliverableStatus.Pending,
                EvidenceUrl = "https://evidence"
            };

            CampaignDeliverable result = await service.UpdateDeliverable(deliverable.Id, request);

            result.EvidenceUrl.Should().Be("https://evidence");
            financial.Verify(item => item.GenerateForPublishedDeliverable(It.IsAny<CampaignDeliverable>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
