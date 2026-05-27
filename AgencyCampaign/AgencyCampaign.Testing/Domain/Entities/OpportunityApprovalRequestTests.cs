using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityApprovalRequestTests
    {
        private static OpportunityApprovalRequest BuildDefault()
        {
            return new OpportunityApprovalRequest(proposalId: 1, approvalType: OpportunityApprovalType.DiscountApproval,
                reason: "  10%  ", requestedByUserName: "  Tester  ", requestedByUserId: 7);
        }

        [Test]
        public void Constructor_should_trim_and_initialize_pending()
        {
            OpportunityApprovalRequest subject = BuildDefault();

            subject.Status.Should().Be(OpportunityApprovalStatus.Pending);
            subject.Reason.Should().Be("10%");
            subject.RequestedByUserName.Should().Be("Tester");
            subject.RequestedByUserId.Should().Be(7);
            subject.DecidedAt.Should().BeNull();
        }

        [Test]
        public void Constructor_should_reject_invalid_inputs()
        {
            Action invalidNegotiation = () => _ = new OpportunityApprovalRequest(0, OpportunityApprovalType.DiscountApproval, "x", "Tester");
            Action blankReason = () => _ = new OpportunityApprovalRequest(1, OpportunityApprovalType.DiscountApproval, " ", "Tester");
            Action blankUser = () => _ = new OpportunityApprovalRequest(1, OpportunityApprovalType.DiscountApproval, "x", " ");

            invalidNegotiation.Should().Throw<ArgumentOutOfRangeException>();
            blankReason.Should().Throw<ArgumentException>();
            blankUser.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Approve_should_set_status_and_clear_decision_metadata()
        {
            OpportunityApprovalRequest subject = BuildDefault();

            subject.Approve("  Boss  ", decisionNotes: "  ok  ", approvedByUserId: 99);

            subject.Status.Should().Be(OpportunityApprovalStatus.Approved);
            subject.ApprovedByUserName.Should().Be("Boss");
            subject.ApprovedByUserId.Should().Be(99);
            subject.DecisionNotes.Should().Be("ok");
            subject.DecidedAt.Should().NotBeNull();
        }

        [Test]
        public void Reject_should_set_status_rejected()
        {
            OpportunityApprovalRequest subject = BuildDefault();

            subject.Reject("Boss", decisionNotes: "fora do escopo");

            subject.Status.Should().Be(OpportunityApprovalStatus.Rejected);
            subject.DecisionNotes.Should().Be("fora do escopo");
            subject.DecidedAt.Should().NotBeNull();
        }

        [Test]
        public void Approve_and_Reject_should_reject_blank_user()
        {
            OpportunityApprovalRequest subject = BuildDefault();

            Action approve = () => subject.Approve(" ");
            Action reject = () => subject.Reject(" ");

            approve.Should().Throw<ArgumentException>();
            reject.Should().Throw<ArgumentException>();
        }

        [Test]
        public void AddReviewer_should_add_pending_reviewer()
        {
            OpportunityApprovalRequest subject = BuildDefault();

            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);

            subject.Reviewers.Should().ContainSingle(item => item.UserName == "Ana" && item.Required && item.Status == OpportunityApprovalReviewerStatus.Pending);
        }

        [Test]
        public void RegisterReviewerDecision_should_keep_pending_until_all_required_approve()
        {
            OpportunityApprovalRequest subject = BuildDefault();
            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            subject.AddReviewer("Bruno", "CFO", required: true, userId: 20);

            subject.RegisterReviewerDecision(10, OpportunityApprovalReviewerStatus.Approved);

            subject.Status.Should().Be(OpportunityApprovalStatus.Pending);
        }

        [Test]
        public void RegisterReviewerDecision_should_approve_when_all_required_approved()
        {
            OpportunityApprovalRequest subject = BuildDefault();
            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            subject.AddReviewer("Bruno", "CFO", required: true, userId: 20);

            subject.RegisterReviewerDecision(10, OpportunityApprovalReviewerStatus.Approved);
            subject.RegisterReviewerDecision(20, OpportunityApprovalReviewerStatus.Approved, "ok");

            subject.Status.Should().Be(OpportunityApprovalStatus.Approved);
            subject.ApprovedByUserId.Should().Be(20);
            subject.DecidedAt.Should().NotBeNull();
        }

        [Test]
        public void RegisterReviewerDecision_should_reject_when_any_required_rejects()
        {
            OpportunityApprovalRequest subject = BuildDefault();
            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            subject.AddReviewer("Bruno", "CFO", required: true, userId: 20);

            subject.RegisterReviewerDecision(10, OpportunityApprovalReviewerStatus.Approved);
            subject.RegisterReviewerDecision(20, OpportunityApprovalReviewerStatus.Rejected, "fora da alcada");

            subject.Status.Should().Be(OpportunityApprovalStatus.Rejected);
            subject.ApprovedByUserId.Should().Be(20);
        }

        [Test]
        public void RegisterReviewerDecision_should_ignore_optional_reviewers_for_quorum()
        {
            OpportunityApprovalRequest subject = BuildDefault();
            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);
            subject.AddReviewer("Carla", "Consultora", required: false, userId: 30);

            subject.RegisterReviewerDecision(10, OpportunityApprovalReviewerStatus.Approved);

            subject.Status.Should().Be(OpportunityApprovalStatus.Approved);
        }

        [Test]
        public void RegisterReviewerDecision_should_throw_when_user_is_not_a_pending_reviewer()
        {
            OpportunityApprovalRequest subject = BuildDefault();
            subject.AddReviewer("Ana", "Diretora", required: true, userId: 10);

            Action act = () => subject.RegisterReviewerDecision(999, OpportunityApprovalReviewerStatus.Approved);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
