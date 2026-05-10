using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityApprovalRequestTests
    {
        private static OpportunityApprovalRequest BuildDefault()
        {
            return new OpportunityApprovalRequest(opportunityNegotiationId: 1, approvalType: OpportunityApprovalType.DiscountApproval,
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
    }
}
