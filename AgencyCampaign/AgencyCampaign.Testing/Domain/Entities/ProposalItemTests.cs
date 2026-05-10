using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ProposalItemTests
    {
        [Test]
        public void Total_should_be_quantity_times_unit_price()
        {
            ProposalItem subject = new(proposalId: 1, description: "x", quantity: 3, unitPrice: 50m);
            subject.Total.Should().Be(150m);
        }

        [Test]
        public void Constructor_should_reject_blank_description()
        {
            Action act = () => _ = new ProposalItem(1, " ", 1, 1m);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Constructor_should_reject_negative_quantity_or_unit_price()
        {
            Action negativeQty = () => _ = new ProposalItem(1, "x", -1, 1m);
            Action negativeUnit = () => _ = new ProposalItem(1, "x", 1, -1m);

            negativeQty.Should().Throw<ArgumentOutOfRangeException>();
            negativeUnit.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void AssignCreator_should_persist_id_and_RemoveCreator_should_clear_it()
        {
            ProposalItem subject = new(1, "x", 1, 10m);

            subject.AssignCreator(7);
            subject.CreatorId.Should().Be(7);

            subject.RemoveCreator();
            subject.CreatorId.Should().BeNull();
        }

        [Test]
        public void AssignCreator_should_reject_invalid_id()
        {
            ProposalItem subject = new(1, "x", 1, 10m);
            Action act = () => subject.AssignCreator(0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ChangeStatus_should_overwrite_status()
        {
            ProposalItem subject = new(1, "x", 1, 10m);
            subject.ChangeStatus(ProposalItemStatus.Confirmed);
            subject.Status.Should().Be(ProposalItemStatus.Confirmed);
        }
    }
}
