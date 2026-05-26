using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityApprovalRequestServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private OpportunityApprovalRequestService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            Mock<IPolicyEvaluator> policyEvaluator = new();
            policyEvaluator
                .Setup(p => p.EvaluateNegotiationAsync(It.IsAny<OpportunityNegotiation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyEvaluationModel { HasDeviations = false });
            service = new OpportunityApprovalRequestService(db, notifications.Object, policyEvaluator.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<OpportunityNegotiation> SeedNegotiationAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            db.Add(new Brand("Acme").WithId(1));
            await db.SaveChangesAsync();
            Opportunity opportunity = new(1, 1, "Big deal", 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();

            OpportunityNegotiation negotiation = new(opportunity.Id, "v1", 100m, DateTimeOffset.UtcNow);
            db.Add(negotiation);
            await db.SaveChangesAsync();
            return negotiation;
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_throw_when_negotiation_not_found()
        {
            CreateOpportunityApprovalRequest request = new()
            {
                OpportunityNegotiationId = 99,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "alta",
                RequestedByUserName = "Tester"
            };

            Func<Task> act = () => service.CreateOpportunityApprovalRequest(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_persist_mark_negotiation_pending_and_notify()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();

            OpportunityApprovalRequest result = await service.CreateOpportunityApprovalRequest(new CreateOpportunityApprovalRequest
            {
                OpportunityNegotiationId = negotiation.Id,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "10%",
                RequestedByUserName = "Tester"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Pending);
            (await db.Set<OpportunityApprovalRequest>().CountAsync()).Should().Be(1);

            db.ChangeTracker.Clear();
            OpportunityNegotiation persisted = await db.Set<OpportunityNegotiation>().AsNoTracking().SingleAsync();
            persisted.Status.Should().Be(OpportunityNegotiationStatus.PendingApproval);

            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_throw_when_negotiation_already_approved()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            negotiation.Approve();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreateOpportunityApprovalRequest request = new()
            {
                OpportunityNegotiationId = negotiation.Id,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "10%",
                RequestedByUserName = "Tester"
            };

            Func<Task> act = () => service.CreateOpportunityApprovalRequest(request);
            await act.Should().ThrowAsync<InvalidOperationException>();

            (await db.Set<OpportunityApprovalRequest>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task Approve_should_set_status_and_mark_negotiation_approved()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            negotiation.MarkPendingApproval();
            await db.SaveChangesAsync();

            OpportunityApprovalRequest approval = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester", 1);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest result = await service.Approve(approval.Id, new DecideOpportunityApprovalRequest
            {
                ApprovedByUserName = "Boss",
                DecisionNotes = "ok"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Approved);
            db.ChangeTracker.Clear();
            (await db.Set<OpportunityNegotiation>().AsNoTracking().SingleAsync()).Status.Should().Be(OpportunityNegotiationStatus.Approved);
        }

        [Test]
        public async Task Approve_should_throw_when_already_decided()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            negotiation.MarkPendingApproval();
            await db.SaveChangesAsync();

            OpportunityApprovalRequest approval = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.Approve("Boss");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Reject_should_set_status_and_mark_negotiation_rejected()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            negotiation.MarkPendingApproval();
            await db.SaveChangesAsync();

            OpportunityApprovalRequest approval = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest result = await service.Reject(approval.Id, new DecideOpportunityApprovalRequest
            {
                ApprovedByUserName = "Boss"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Rejected);
            (await db.Set<OpportunityNegotiation>().AsNoTracking().SingleAsync()).Status.Should().Be(OpportunityNegotiationStatus.Rejected);
        }

        [Test]
        public async Task Approve_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Approve(99, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Reject_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Reject(99, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Reject_should_throw_when_already_decided()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            negotiation.MarkPendingApproval();
            await db.SaveChangesAsync();

            OpportunityApprovalRequest approval = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.Reject("Boss");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.Reject(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_notify_each_approver_when_user_ids_provided()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();

            await service.CreateOpportunityApprovalRequest(new CreateOpportunityApprovalRequest
            {
                OpportunityNegotiationId = negotiation.Id,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "alta",
                RequestedByUserName = "Tester",
                RequestedByUserId = 1,
                ApproverUserIds = new List<long> { 10, 20, 30 }
            });

            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task GetOpportunityApprovalRequestById_should_return_null_when_not_found()
        {
            (await service.GetOpportunityApprovalRequestById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetOpportunityApprovalRequestById_should_return_when_found()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            OpportunityApprovalRequest approval = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "x", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();

            OpportunityApprovalRequest? result = await service.GetOpportunityApprovalRequestById(approval.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetAllApprovals_should_return_paged_result()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            db.Add(new OpportunityApprovalRequest(negotiation.Id, OpportunityApprovalType.DiscountApproval, "a", "Tester"));
            db.Add(new OpportunityApprovalRequest(negotiation.Id, OpportunityApprovalType.DeadlineApproval, "b", "Tester"));
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<OpportunityApprovalRequest> result = await service.GetAllApprovals(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetApprovalsSummary_should_aggregate_by_status()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();
            OpportunityApprovalRequest pending = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "pending", "Tester");
            OpportunityApprovalRequest approved = new(negotiation.Id, OpportunityApprovalType.DeadlineApproval, "approved", "Tester");
            approved.Approve("Boss");
            OpportunityApprovalRequest rejected = new(negotiation.Id, OpportunityApprovalType.DiscountApproval, "rejected", "Tester");
            rejected.Reject("Boss");

            db.Add(pending);
            db.Add(approved);
            db.Add(rejected);
            await db.SaveChangesAsync();

            var summary = await service.GetApprovalsSummary();

            summary.Pending.Should().Be(1);
            summary.Approved.Should().Be(1);
            summary.Rejected.Should().Be(1);
        }

        [Test]
        public async Task GetApprovalsByNegotiationId_should_filter_and_order_desc()
        {
            OpportunityNegotiation negotiation = await SeedNegotiationAsync();

            db.Add(new OpportunityApprovalRequest(negotiation.Id, OpportunityApprovalType.DiscountApproval, "v1", "Tester").WithId(1));
            db.Add(new OpportunityApprovalRequest(negotiation.Id, OpportunityApprovalType.DeadlineApproval, "v2", "Tester").WithId(2));
            db.Add(new OpportunityApprovalRequest(99, OpportunityApprovalType.DiscountApproval, "outro", "Tester").WithId(3));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityApprovalRequest> result = await service.GetApprovalsByNegotiationId(negotiation.Id);

            result.Select(item => item.Id).Should().Equal(2, 1);
        }
    }
}
