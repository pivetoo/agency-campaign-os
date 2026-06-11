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

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityApprovalRequestServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private Mock<IPolicyEvaluator> policyEvaluator = null!;
        private OpportunityApprovalRequestService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            policyEvaluator = new();
            policyEvaluator
                .Setup(p => p.EvaluateProposalAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyEvaluationModel { HasDeviations = false });
            service = new OpportunityApprovalRequestService(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Proposal> SeedProposalAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            db.Add(new Brand("Acme").WithId(1));
            await db.SaveChangesAsync();
            Opportunity opportunity = new(1, 1, "Big deal", 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();

            Proposal proposal = new(opportunity.Id, "Proposta v1", 1);
            proposal.UpdateTotalValue(1000m);
            db.Add(proposal);
            await db.SaveChangesAsync();
            return proposal;
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_throw_when_proposal_not_found()
        {
            CreateOpportunityApprovalRequest request = new()
            {
                ProposalId = 99,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "alta",
                RequestedByUserName = "Tester"
            };

            Func<Task> act = () => service.CreateOpportunityApprovalRequest(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_persist_pending_and_notify()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest result = await service.CreateOpportunityApprovalRequest(new CreateOpportunityApprovalRequest
            {
                ProposalId = proposal.Id,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "10%",
                RequestedByUserName = "Tester"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Pending);
            result.ProposalId.Should().Be(proposal.Id);
            (await db.Set<OpportunityApprovalRequest>().CountAsync()).Should().Be(1);

            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Approve_should_set_status_approved()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Solicitante", 2);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest result = await service.Approve(approval.Id, new DecideOpportunityApprovalRequest
            {
                ApprovedByUserName = "Boss",
                DecisionNotes = "ok"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Approved);
        }

        [Test]
        public async Task Approve_should_throw_when_already_decided()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.Approve("Boss");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Approve_should_record_authenticated_user_not_request_body()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequestService boss = new(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create(userId: 7, userName: "Aprovador Real"));
            OpportunityApprovalRequest result = await boss.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Nome Forjado", ApprovedByUserId = 999 });

            result.ApprovedByUserName.Should().Be("Aprovador Real");
            result.ApprovedByUserId.Should().Be(7);
        }

        [Test]
        public async Task Approve_should_throw_when_required_reviewers_exist()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Reject_should_set_status_rejected()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest result = await service.Reject(approval.Id, new DecideOpportunityApprovalRequest
            {
                ApprovedByUserName = "Boss"
            });

            result.Status.Should().Be(OpportunityApprovalStatus.Rejected);
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
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.Reject("Boss");
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.Reject(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityApprovalRequest_should_create_reviewers_and_notify_each_approver()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest created = await service.CreateOpportunityApprovalRequest(new CreateOpportunityApprovalRequest
            {
                ProposalId = proposal.Id,
                ApprovalType = OpportunityApprovalType.DiscountApproval,
                Reason = "alta",
                RequestedByUserName = "Tester",
                RequestedByUserId = 1,
                Approvers = new List<ApproverRequest>
                {
                    new() { UserId = 10, UserName = "Ana" },
                    new() { UserId = 20, UserName = "Bruno" },
                    new() { UserId = 30, UserName = "Carla" },
                }
            });

            (await db.Set<OpportunityApprovalReviewer>().CountAsync(item => item.OpportunityApprovalRequestId == created.Id)).Should().Be(3);
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
            Proposal proposal = await SeedProposalAsync();
            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "x", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();

            OpportunityApprovalRequest? result = await service.GetOpportunityApprovalRequestById(approval.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetAllApprovals_should_return_paged_result()
        {
            Proposal proposal = await SeedProposalAsync();
            db.Add(new OpportunityApprovalRequest(proposal.Id, OpportunityApprovalType.DiscountApproval, "a", "Tester"));
            db.Add(new OpportunityApprovalRequest(proposal.Id, OpportunityApprovalType.DeadlineApproval, "b", "Tester"));
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<OpportunityApprovalRequest> result = await service.GetAllApprovals(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetApprovalsSummary_should_aggregate_by_status()
        {
            Proposal proposal = await SeedProposalAsync();
            OpportunityApprovalRequest pending = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "pending", "Tester");
            OpportunityApprovalRequest approved = new(proposal.Id, OpportunityApprovalType.DeadlineApproval, "approved", "Tester");
            approved.Approve("Boss");
            OpportunityApprovalRequest rejected = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "rejected", "Tester");
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
        public async Task RecordReviewerDecision_should_keep_request_pending_until_all_required_reviewers_approve()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            approval.AddReviewer("Bruno", "CFO", required: true, userId: 20);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequestService ana = new(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create(userId: 10, userName: "Ana"));
            OpportunityApprovalRequest result = await ana.RecordReviewerDecision(approval.Id, OpportunityApprovalReviewerStatus.Approved);

            result.Status.Should().Be(OpportunityApprovalStatus.Pending);
        }

        [Test]
        public async Task RecordReviewerDecision_should_approve_request_when_all_required_approve()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            approval.AddReviewer("Bruno", "CFO", required: true, userId: 20);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequestService ana = new(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create(userId: 10, userName: "Ana"));
            await ana.RecordReviewerDecision(approval.Id, OpportunityApprovalReviewerStatus.Approved);
            db.ChangeTracker.Clear();

            OpportunityApprovalRequestService bruno = new(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create(userId: 20, userName: "Bruno"));
            OpportunityApprovalRequest result = await bruno.RecordReviewerDecision(approval.Id, OpportunityApprovalReviewerStatus.Approved, "ok");

            result.Status.Should().Be(OpportunityApprovalStatus.Approved);
        }

        [Test]
        public async Task RecordReviewerDecision_should_throw_when_current_user_is_not_a_reviewer()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequestService stranger = new(db, notifications.Object, policyEvaluator.Object, CurrentUserMock.Create(userId: 999, userName: "Estranho"));
            Func<Task> act = () => stranger.RecordReviewerDecision(approval.Id, OpportunityApprovalReviewerStatus.Approved);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetApprovalsByProposalId_should_filter_and_order_desc()
        {
            Proposal proposal = await SeedProposalAsync();

            db.Add(new OpportunityApprovalRequest(proposal.Id, OpportunityApprovalType.DiscountApproval, "v1", "Tester").WithId(1));
            db.Add(new OpportunityApprovalRequest(proposal.Id, OpportunityApprovalType.DeadlineApproval, "v2", "Tester").WithId(2));
            db.Add(new OpportunityApprovalRequest(99, OpportunityApprovalType.DiscountApproval, "outro", "Tester").WithId(3));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityApprovalRequest> result = await service.GetApprovalsByProposalId(proposal.Id);

            result.Select(item => item.Id).Should().Equal(2, 1);
        }

        [Test]
        public async Task GetApprovalsByProposalId_should_include_reviewers()
        {
            Proposal proposal = await SeedProposalAsync();

            OpportunityApprovalRequest approval = new(proposal.Id, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            approval.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            db.Add(approval);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            IReadOnlyCollection<OpportunityApprovalRequest> result = await service.GetApprovalsByProposalId(proposal.Id);

            result.Should().ContainSingle();
            result.Single().Reviewers.Should().ContainSingle(reviewer => reviewer.UserName == "Ana");
        }
    }
}
