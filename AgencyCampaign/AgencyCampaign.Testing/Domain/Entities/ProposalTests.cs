using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ProposalTests
    {
        private static Proposal BuildDefault()
        {
            return new Proposal(opportunityId: 1, name: "  Proposta  ", internalOwnerId: 5);
        }

        private static ProposalItem BuildItem(long id, int qty = 1, decimal unit = 100m)
        {
            return new ProposalItem(proposalId: 1, description: "x", quantity: qty, unitPrice: unit).WithId(id);
        }

        [Test]
        public void Constructor_should_initialize_with_draft_and_seed_status_history()
        {
            Proposal subject = BuildDefault();

            subject.Status.Should().Be(ProposalStatus.Draft);
            subject.Name.Should().Be("Proposta");
            subject.StatusHistory.Should().HaveCount(1);
            subject.StatusHistory.Single().ToStatus.Should().Be(ProposalStatus.Draft);
        }

        [Test]
        public void Constructor_should_reject_invalid_inputs()
        {
            Action invalidOpportunity = () => _ = new Proposal(0, "x", 1);
            Action blankName = () => _ = new Proposal(1, " ", 1);
            Action invalidOwner = () => _ = new Proposal(1, "x", 0);

            invalidOpportunity.Should().Throw<ArgumentOutOfRangeException>();
            blankName.Should().Throw<ArgumentException>();
            invalidOwner.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void MarkAsViewed_should_require_sent_status()
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.MarkAsViewed();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Approve_should_require_sent_or_viewed_status()
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.Approve();
            act.Should().Throw<InvalidOperationException>();

            subject.MarkAsSent();
            subject.Approve();
            subject.Status.Should().Be(ProposalStatus.Approved);
        }

        [Test]
        public void Reject_should_require_sent_or_viewed_status()
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.Reject();
            act.Should().Throw<InvalidOperationException>();

            subject.MarkAsSent();
            subject.MarkAsViewed();
            subject.Reject();
            subject.Status.Should().Be(ProposalStatus.Rejected);
        }

        [Test]
        public void ConvertToCampaign_should_require_approved_status()
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.ConvertToCampaign(99);
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void ConvertToCampaign_should_set_campaign_id_when_approved()
        {
            Proposal subject = BuildDefault();
            subject.MarkAsSent();
            subject.Approve();

            subject.ConvertToCampaign(99);

            subject.CampaignId.Should().Be(99);
            subject.Status.Should().Be(ProposalStatus.Converted);
        }

        [Test]
        public void Cancel_should_throw_when_already_converted()
        {
            Proposal subject = BuildDefault();
            subject.MarkAsSent();
            subject.Approve();
            subject.ConvertToCampaign(99);

            Action act = () => subject.Cancel();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Cancel_should_register_status_change()
        {
            Proposal subject = BuildDefault();

            subject.Cancel(reason: "desistência");

            subject.Status.Should().Be(ProposalStatus.Cancelled);
            subject.StatusHistory.Should().HaveCount(2);
        }

        [Test]
        public void Expire_should_set_expired_status_only_when_sent_and_validity_past()
        {
            Proposal sentExpired = BuildDefault();
            sentExpired.Update("x", validityUntil: DateTimeOffset.UtcNow.AddMinutes(-5), description: null, notes: null, opportunityId: 1);
            sentExpired.MarkAsSent();

            sentExpired.Expire();

            sentExpired.Status.Should().Be(ProposalStatus.Expired);
        }

        [Test]
        public void Expire_should_be_no_op_when_not_sent_or_validity_in_future()
        {
            Proposal noValidity = BuildDefault();
            noValidity.Expire();
            noValidity.Status.Should().Be(ProposalStatus.Draft);

            Proposal futureValidity = BuildDefault();
            futureValidity.Update("x", validityUntil: DateTimeOffset.UtcNow.AddDays(1), description: null, notes: null, opportunityId: 1);
            futureValidity.MarkAsSent();
            futureValidity.Expire();
            futureValidity.Status.Should().Be(ProposalStatus.Sent);
        }

        [Test]
        public void AddItem_should_recalculate_total()
        {
            Proposal subject = BuildDefault();

            subject.AddItem(BuildItem(1, qty: 2, unit: 100m));
            subject.AddItem(BuildItem(2, qty: 1, unit: 50m));

            subject.Items.Should().HaveCount(2);
            subject.TotalValue.Should().Be(250m);
        }

        [Test]
        public void AddItem_should_reject_duplicate_id()
        {
            Proposal subject = BuildDefault();
            subject.AddItem(BuildItem(1));

            Action act = () => subject.AddItem(BuildItem(1));
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void RemoveItem_should_throw_when_item_not_found()
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.RemoveItem(99);
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void RemoveItem_should_recalculate_total()
        {
            Proposal subject = BuildDefault();
            subject.AddItem(BuildItem(1, unit: 100m));
            subject.AddItem(BuildItem(2, unit: 200m));

            subject.RemoveItem(1);

            subject.Items.Should().HaveCount(1);
            subject.TotalValue.Should().Be(200m);
        }

        [Test]
        public void SetInternalOwner_should_persist_id_and_name()
        {
            Proposal subject = BuildDefault();

            subject.SetInternalOwner(7, "Alice");

            subject.InternalOwnerId.Should().Be(7);
            subject.InternalOwnerName.Should().Be("Alice");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void SetInternalOwner_should_reject_invalid_user_id(long userId)
        {
            Proposal subject = BuildDefault();
            Action act = () => subject.SetInternalOwner(userId, "x");
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ChangeStatus_to_same_status_should_not_add_history_entry()
        {
            Proposal subject = BuildDefault();
            int historyBefore = subject.StatusHistory.Count;

            subject.ChangeStatus(ProposalStatus.Draft);

            subject.StatusHistory.Count.Should().Be(historyBefore);
        }
    }
}
