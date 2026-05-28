using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Builders
{
    public sealed class ProposalBuilder
    {
        private long id = 1;
        private long opportunityId = 1;
        private string name = "Proposta v1";
        private long internalOwnerId = 1;
        private string? description;
        private DateTimeOffset? validityUntil;
        private string? notes;
        private decimal? discountAmount;
        private int? paymentTermDays;
        private decimal totalValue = 1000m;

        public ProposalBuilder WithId(long value) { id = value; return this; }
        public ProposalBuilder WithOpportunityId(long value) { opportunityId = value; return this; }
        public ProposalBuilder WithName(string value) { name = value; return this; }
        public ProposalBuilder WithInternalOwnerId(long value) { internalOwnerId = value; return this; }
        public ProposalBuilder WithDiscountAmount(decimal? value) { discountAmount = value; return this; }
        public ProposalBuilder WithPaymentTermDays(int? value) { paymentTermDays = value; return this; }
        public ProposalBuilder WithTotalValue(decimal value) { totalValue = value; return this; }

        public Proposal Build()
        {
            Proposal proposal = new(opportunityId, name, internalOwnerId, description, validityUntil, notes, internalOwnerId, "Tester", discountAmount, paymentTermDays);
            proposal.UpdateTotalValue(totalValue);
            return proposal.WithId(id);
        }
    }
}
