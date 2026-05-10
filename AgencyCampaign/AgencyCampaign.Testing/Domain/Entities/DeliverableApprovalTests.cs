using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class DeliverableApprovalTests
    {
        private static DeliverableApproval BuildDefault()
        {
            return new DeliverableApproval(campaignDeliverableId: 1, approvalType: DeliverableApprovalType.Brand, reviewerName: "  Reviewer  ");
        }

        [Test]
        public void Constructor_should_initialize_pending_with_trimmed_reviewer()
        {
            DeliverableApproval subject = BuildDefault();

            subject.Status.Should().Be(DeliverableApprovalStatus.Pending);
            subject.ReviewerName.Should().Be("Reviewer");
            subject.ApprovedAt.Should().BeNull();
            subject.RejectedAt.Should().BeNull();
        }

        [Test]
        public void Approve_should_set_status_and_clear_rejection()
        {
            DeliverableApproval subject = BuildDefault();
            subject.Reject("ruim");

            subject.Approve(comment: " ok ", approvedAt: DateTimeOffset.UtcNow);

            subject.Status.Should().Be(DeliverableApprovalStatus.Approved);
            subject.Comment.Should().Be("ok");
            subject.RejectedAt.Should().BeNull();
            subject.ApprovedAt.Should().NotBeNull();
        }

        [Test]
        public void Reject_should_set_status_and_clear_approval()
        {
            DeliverableApproval subject = BuildDefault();
            subject.Approve();

            subject.Reject(comment: " ruim ");

            subject.Status.Should().Be(DeliverableApprovalStatus.Rejected);
            subject.Comment.Should().Be("ruim");
            subject.ApprovedAt.Should().BeNull();
            subject.RejectedAt.Should().NotBeNull();
        }

        [Test]
        public void Reset_should_clear_status_and_timestamps()
        {
            DeliverableApproval subject = BuildDefault();
            subject.Approve(comment: "ok");

            subject.Reset(comment: "  reabrindo  ");

            subject.Status.Should().Be(DeliverableApprovalStatus.Pending);
            subject.Comment.Should().Be("reabrindo");
            subject.ApprovedAt.Should().BeNull();
            subject.RejectedAt.Should().BeNull();
        }

        [Test]
        public void UpdateReviewer_should_reject_blank()
        {
            DeliverableApproval subject = BuildDefault();
            Action act = () => subject.UpdateReviewer(" ");
            act.Should().Throw<ArgumentException>();
        }
    }
}
