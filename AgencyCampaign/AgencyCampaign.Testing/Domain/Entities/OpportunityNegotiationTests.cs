using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityNegotiationTests
    {
        private static OpportunityNegotiation BuildDefault()
        {
            return new OpportunityNegotiation(opportunityId: 1, title: "  v1  ", amount: 1000m, negotiatedAt: DateTimeOffset.UtcNow);
        }

        [Test]
        public void Constructor_should_initialize_with_draft_status()
        {
            OpportunityNegotiation subject = BuildDefault();

            subject.Status.Should().Be(OpportunityNegotiationStatus.Draft);
            subject.Title.Should().Be("v1");
        }

        [Test]
        public void MarkPendingApproval_should_throw_when_negotiation_already_approved()
        {
            OpportunityNegotiation subject = BuildDefault();
            subject.Approve();

            Action act = () => subject.MarkPendingApproval();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void MarkPendingApproval_should_throw_when_already_accepted_by_client()
        {
            OpportunityNegotiation subject = BuildDefault();
            subject.Approve();
            subject.MarkAcceptedByClient();

            Action act = () => subject.MarkPendingApproval();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void MarkSentToClient_should_require_approved_status()
        {
            OpportunityNegotiation subject = BuildDefault();

            Action act = () => subject.MarkSentToClient();
            act.Should().Throw<InvalidOperationException>();

            subject.Approve();
            subject.MarkSentToClient();
            subject.Status.Should().Be(OpportunityNegotiationStatus.SentToClient);
        }

        [Test]
        public void MarkAcceptedByClient_should_require_approved_or_sent_status()
        {
            OpportunityNegotiation subject = BuildDefault();
            Action act = () => subject.MarkAcceptedByClient();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void MarkAcceptedByClient_should_succeed_after_approval()
        {
            OpportunityNegotiation subject = BuildDefault();
            subject.Approve();

            subject.MarkAcceptedByClient();

            subject.Status.Should().Be(OpportunityNegotiationStatus.AcceptedByClient);
        }

        [Test]
        public void Reject_should_set_status_regardless_of_previous_state()
        {
            OpportunityNegotiation subject = BuildDefault();
            subject.MarkPendingApproval();

            subject.Reject();

            subject.Status.Should().Be(OpportunityNegotiationStatus.Rejected);
        }

        [Test]
        public void Update_should_replace_metadata_and_touch_updatedAt()
        {
            OpportunityNegotiation subject = BuildDefault();
            DateTimeOffset before = subject.UpdatedAt!.Value;
            Thread.Sleep(2);

            subject.Update("v2", 2000m, DateTimeOffset.UtcNow, "  notas  ");

            subject.Title.Should().Be("v2");
            subject.Amount.Should().Be(2000m);
            subject.Notes.Should().Be("notas");
            subject.UpdatedAt!.Value.Should().BeAfter(before);
        }
    }
}
