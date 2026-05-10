using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Deliverables;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.DeliverableShareLinks;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class DeliverableShareLinkServiceTests
    {
        private TestDbContext db = null!;
        private DeliverableShareLinkService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new DeliverableShareLinkService(db, CurrentUserMock.Create(), LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<CampaignDeliverable> SeedDeliverableAsync()
        {
            CampaignDeliverable deliverable = new(
                campaignId: 1, campaignCreatorId: 1, title: "x", deliverableKindId: 1, platformId: 1,
                dueAt: DateTimeOffset.UtcNow, grossAmount: 1000m, creatorAmount: 800m, agencyFeeAmount: 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            return deliverable;
        }

        [Test]
        public async Task Create_should_throw_when_deliverable_not_found()
        {
            CreateDeliverableShareLinkRequest request = new()
            {
                CampaignDeliverableId = 99,
                ReviewerName = "Brand"
            };

            Func<Task> act = () => service.Create(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Create_should_persist_with_unique_token()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();

            DeliverableShareLinkModel a = await service.Create(new CreateDeliverableShareLinkRequest { CampaignDeliverableId = deliverable.Id, ReviewerName = "Brand A" });
            DeliverableShareLinkModel b = await service.Create(new CreateDeliverableShareLinkRequest { CampaignDeliverableId = deliverable.Id, ReviewerName = "Brand B" });

            a.Token.Should().NotBe(b.Token);
            a.Token.Should().NotBeNullOrWhiteSpace();
            a.IsActive.Should().BeTrue();
        }

        [Test]
        public async Task Revoke_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Revoke(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Revoke_should_mark_share_link_as_revoked()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            DeliverableShareLink link = new(deliverable.Id, "tok", "Reviewer", null, null, null);
            db.Add(link);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.Revoke(link.Id);

            DeliverableShareLink persisted = await db.Set<DeliverableShareLink>().AsNoTracking().SingleAsync();
            persisted.RevokedAt.Should().NotBeNull();
        }

        [Test]
        public async Task GetByDeliverable_should_compute_is_active_from_revocation_and_expiration()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            DeliverableShareLink active = new(deliverable.Id, "active", "A", now.AddDays(7), null, null);
            DeliverableShareLink expired = new(deliverable.Id, "expired", "E", now.AddMinutes(-1), null, null);
            DeliverableShareLink revoked = new(deliverable.Id, "revoked", "R", null, null, null);
            revoked.Revoke();

            db.Add(active);
            db.Add(expired);
            db.Add(revoked);
            await db.SaveChangesAsync();

            IReadOnlyCollection<DeliverableShareLinkModel> result = await service.GetByDeliverable(deliverable.Id);

            result.Single(item => item.Token == "active").IsActive.Should().BeTrue();
            result.Single(item => item.Token == "expired").IsActive.Should().BeFalse();
            result.Single(item => item.Token == "revoked").IsActive.Should().BeFalse();
        }
    }

    [TestFixture]
    public sealed class DeliverablePublicServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private DeliverablePublicService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            service = new DeliverablePublicService(db, LocalizerMock.Create<AgencyCampaignResource>(), notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<(CampaignDeliverable deliverable, DeliverableShareLink link)> SeedAsync(DateTimeOffset? expiresAt = null, bool revoke = false)
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();

            Campaign campaign = new(brand.Id, "Camp", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            CampaignCreator campaignCreator = new(campaign.Id, creator.Id, 1, 100m, 10m);
            db.Add(campaignCreator);
            DeliverableKind kind = new("Story");
            db.Add(kind);
            Platform platform = new("Instagram");
            db.Add(platform);
            await db.SaveChangesAsync();

            CampaignDeliverable deliverable = new(
                campaignId: campaign.Id, campaignCreatorId: campaignCreator.Id, title: "x", deliverableKindId: kind.Id, platformId: platform.Id,
                dueAt: DateTimeOffset.UtcNow, grossAmount: 1000m, creatorAmount: 800m, agencyFeeAmount: 100m);
            db.Add(deliverable);
            await db.SaveChangesAsync();

            DeliverableShareLink link = new(deliverable.Id, "tok", "Brand", expiresAt, null, null);
            if (revoke)
            {
                link.Revoke();
            }
            db.Add(link);
            await db.SaveChangesAsync();
            return (deliverable, link);
        }

        [Test]
        public async Task GetByToken_should_return_null_for_blank_or_unknown_token()
        {
            (await service.GetByToken(" ")).Should().BeNull();
            (await service.GetByToken("missing")).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_return_null_for_inactive_link()
        {
            await SeedAsync(revoke: true);
            (await service.GetByToken("tok")).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_register_view()
        {
            await SeedAsync();

            DeliverablePublicViewModel? viewModel = await service.GetByToken("tok");

            viewModel.Should().NotBeNull();
            DeliverableShareLink persisted = await db.Set<DeliverableShareLink>().AsNoTracking().SingleAsync();
            persisted.ViewCount.Should().Be(1);
        }

        [Test]
        public async Task Approve_should_create_brand_approval_and_notify()
        {
            await SeedAsync();

            DeliverablePublicViewModel result = await service.Approve("tok", new PublicDeliverableDecisionRequest { ReviewerName = "Brand", Comment = "Looks good" });

            result.ApprovalStatus.Should().Be((int)DeliverableApprovalStatus.Approved);
            (await db.Set<DeliverableApproval>().CountAsync()).Should().Be(1);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Reject_should_update_existing_brand_approval()
        {
            (CampaignDeliverable deliverable, _) = await SeedAsync();
            db.Add(new DeliverableApproval(deliverable.Id, DeliverableApprovalType.Brand, "Old"));
            await db.SaveChangesAsync();

            DeliverablePublicViewModel result = await service.Reject("tok", new PublicDeliverableDecisionRequest { ReviewerName = "Brand", Comment = "Não" });

            result.ApprovalStatus.Should().Be((int)DeliverableApprovalStatus.Rejected);
            (await db.Set<DeliverableApproval>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Approve_should_throw_when_share_link_inactive()
        {
            await SeedAsync(revoke: true);

            Func<Task> act = () => service.Approve("tok", new PublicDeliverableDecisionRequest { ReviewerName = "Brand" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
