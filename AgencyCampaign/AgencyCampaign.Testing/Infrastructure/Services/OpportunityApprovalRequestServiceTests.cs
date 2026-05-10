using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

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
            service = new OpportunityApprovalRequestService(db, LocalizerMock.Create<AgencyCampaignResource>(), notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<OpportunityNegotiation> SeedNegotiationAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
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
        public async Task CreateOpportunityApprovalRequest_should_persist_and_notify()
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
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
