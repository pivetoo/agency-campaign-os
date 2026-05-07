using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityTagAssignment : Entity
    {
        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public long OpportunityTagId { get; private set; }

        public OpportunityTag? OpportunityTag { get; private set; }

        private OpportunityTagAssignment()
        {
        }

        public OpportunityTagAssignment(long opportunityId, long opportunityTagId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityTagId);

            OpportunityId = opportunityId;
            OpportunityTagId = opportunityTagId;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
