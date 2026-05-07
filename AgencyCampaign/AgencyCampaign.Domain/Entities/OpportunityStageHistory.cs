using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityStageHistory : Entity
    {
        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public long? FromStageId { get; private set; }

        public CommercialPipelineStage? FromStage { get; private set; }

        public long ToStageId { get; private set; }

        public CommercialPipelineStage? ToStage { get; private set; }

        public DateTimeOffset ChangedAt { get; private set; }

        public long? ChangedByUserId { get; private set; }

        public string? ChangedByUserName { get; private set; }

        public string? Reason { get; private set; }

        private OpportunityStageHistory()
        {
        }

        public OpportunityStageHistory(long opportunityId, long? fromStageId, long toStageId, long? changedByUserId, string? changedByUserName, string? reason)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(opportunityId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toStageId);

            OpportunityId = opportunityId;
            FromStageId = fromStageId;
            ToStageId = toStageId;
            ChangedAt = DateTimeOffset.UtcNow;
            ChangedByUserId = changedByUserId;
            ChangedByUserName = Normalize(changedByUserName);
            Reason = Normalize(reason);
            CreatedAt = ChangedAt;
            UpdatedAt = ChangedAt;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
