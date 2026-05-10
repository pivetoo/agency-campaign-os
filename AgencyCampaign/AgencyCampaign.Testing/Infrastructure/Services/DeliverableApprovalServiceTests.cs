using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableApprovals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class DeliverableApprovalServiceTests
    {
        private TestDbContext db = null!;
        private DeliverableApprovalService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new DeliverableApprovalService(db, LocalizerMock.Create<AgencyCampaignResource>());
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
        public async Task CreateApproval_should_throw_when_deliverable_not_found()
        {
            CreateDeliverableApprovalRequest request = new()
            {
                CampaignDeliverableId = 99,
                ApprovalType = DeliverableApprovalType.Brand,
                ReviewerName = "Reviewer"
            };

            Func<Task> act = () => service.CreateApproval(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateApproval_should_reject_duplicate_approval_type_per_deliverable()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            db.Add(new DeliverableApproval(deliverable.Id, DeliverableApprovalType.Brand, "Reviewer A"));
            await db.SaveChangesAsync();

            CreateDeliverableApprovalRequest request = new()
            {
                CampaignDeliverableId = deliverable.Id,
                ApprovalType = DeliverableApprovalType.Brand,
                ReviewerName = "Reviewer B"
            };

            Func<Task> act = () => service.CreateApproval(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateApproval_should_allow_different_approval_types_per_deliverable()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            db.Add(new DeliverableApproval(deliverable.Id, DeliverableApprovalType.Brand, "Brand"));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            DeliverableApproval result = await service.CreateApproval(new CreateDeliverableApprovalRequest
            {
                CampaignDeliverableId = deliverable.Id,
                ApprovalType = DeliverableApprovalType.Internal,
                ReviewerName = "Internal"
            });

            result.ApprovalType.Should().Be(DeliverableApprovalType.Internal);
        }

        [Test]
        public async Task UpdateApproval_should_throw_when_id_mismatch()
        {
            UpdateDeliverableApprovalRequest request = new() { Id = 5, ReviewerName = "x", Status = DeliverableApprovalStatus.Approved };

            Func<Task> act = () => service.UpdateApproval(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateApproval_should_throw_when_not_found()
        {
            UpdateDeliverableApprovalRequest request = new() { Id = 1, ReviewerName = "x", Status = DeliverableApprovalStatus.Approved };

            Func<Task> act = () => service.UpdateApproval(1, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateApproval_should_apply_status_and_comment_for_approved()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            DeliverableApproval approval = new(deliverable.Id, DeliverableApprovalType.Brand, "Reviewer");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            DeliverableApproval result = await service.UpdateApproval(approval.Id, new UpdateDeliverableApprovalRequest
            {
                Id = approval.Id,
                ReviewerName = "New",
                Status = DeliverableApprovalStatus.Approved,
                Comment = "  ok  "
            });

            result.Status.Should().Be(DeliverableApprovalStatus.Approved);
            result.ReviewerName.Should().Be("New");
            result.Comment.Should().Be("ok");
            result.ApprovedAt.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateApproval_to_pending_should_reset_state()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            DeliverableApproval approval = new(deliverable.Id, DeliverableApprovalType.Brand, "Reviewer");
            approval.Approve("ok", DateTimeOffset.UtcNow);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            DeliverableApproval result = await service.UpdateApproval(approval.Id, new UpdateDeliverableApprovalRequest
            {
                Id = approval.Id,
                ReviewerName = "Reviewer",
                Status = DeliverableApprovalStatus.Pending
            });

            result.Status.Should().Be(DeliverableApprovalStatus.Pending);
            result.ApprovedAt.Should().BeNull();
            result.RejectedAt.Should().BeNull();
        }

        [Test]
        public async Task GetByDeliverable_should_filter_by_deliverable_and_order_by_id_desc()
        {
            CampaignDeliverable deliverable = await SeedDeliverableAsync();
            db.Add(new DeliverableApproval(deliverable.Id, DeliverableApprovalType.Brand, "A").WithId(1));
            db.Add(new DeliverableApproval(deliverable.Id, DeliverableApprovalType.Internal, "B").WithId(2));
            db.Add(new DeliverableApproval(99, DeliverableApprovalType.Brand, "Other").WithId(3));
            await db.SaveChangesAsync();

            List<DeliverableApproval> result = await service.GetByDeliverable(deliverable.Id);

            result.Select(item => item.ReviewerName).Should().Equal("B", "A");
        }
    }
}
