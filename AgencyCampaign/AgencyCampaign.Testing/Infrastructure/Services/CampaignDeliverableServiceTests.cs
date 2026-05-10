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
            service = new CampaignDeliverableService(db, LocalizerMock.Create<AgencyCampaignResource>(), financial.Object);
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
            DeliverableApproval approval = new(0, DeliverableApprovalType.Brand, "Brand");
            approval.Approve();
            db.Add(deliverable);
            await db.SaveChangesAsync();

            // Reattach approval with proper FK after deliverable Id is generated
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
    }
}
