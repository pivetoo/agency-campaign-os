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
        public void Total_should_round_to_two_decimals()
        {
            ProposalItem subject = new(proposalId: 1, description: "x", quantity: 3, unitPrice: 10.005m);
            subject.Total.Should().Be(30.02m);
        }

        [Test]
        public void UsageRights_item_should_keep_duration_and_scope()
        {
            ProposalItem subject = new(1, "Usage rights", 1, 5000m, kind: ProposalItemKind.UsageRights, usageDurationMonths: 6, usageScope: "Paid social");

            subject.Kind.Should().Be(ProposalItemKind.UsageRights);
            subject.UsageDurationMonths.Should().Be(6);
            subject.UsageScope.Should().Be("Paid social");
        }

        [Test]
        public void Deliverable_item_should_not_carry_usage_fields()
        {
            ProposalItem subject = new(1, "Reel", 1, 3000m, kind: ProposalItemKind.Deliverable, usageDurationMonths: 6, usageScope: "Paid social");

            subject.UsageDurationMonths.Should().BeNull();
            subject.UsageScope.Should().BeNull();
        }

        [Test]
        public void Commission_item_total_should_be_rate_over_basis()
        {
            ProposalItem subject = new(1, "Comissao de vendas", 1, 0m, pricingModel: ProposalItemPricingModel.Commission, variableRate: 10m, variableBasis: 50000m);

            subject.IsVariable.Should().BeTrue();
            subject.Total.Should().Be(5000m);
            subject.VariableRate.Should().Be(10m);
            subject.VariableBasis.Should().Be(50000m);
        }

        [Test]
        public void Fixed_item_should_not_carry_variable_fields()
        {
            ProposalItem subject = new(1, "Reel", 2, 1500m, pricingModel: ProposalItemPricingModel.Fixed, variableRate: 10m, variableBasis: 50000m);

            subject.IsVariable.Should().BeFalse();
            subject.VariableRate.Should().BeNull();
            subject.VariableBasis.Should().BeNull();
            subject.Total.Should().Be(3000m);
        }

        [Test]
        public void Variable_item_with_missing_basis_should_total_zero()
        {
            ProposalItem subject = new(1, "Performance", 1, 0m, pricingModel: ProposalItemPricingModel.Performance, variableRate: 15m, variableBasis: null);

            subject.Total.Should().Be(0m);
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
