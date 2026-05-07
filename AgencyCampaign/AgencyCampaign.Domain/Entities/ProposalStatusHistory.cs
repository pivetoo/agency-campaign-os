using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalStatusHistory : Entity
    {
        public long ProposalId { get; private set; }

        public Proposal? Proposal { get; private set; }

        public ProposalStatus? FromStatus { get; private set; }

        public ProposalStatus ToStatus { get; private set; }

        public DateTimeOffset ChangedAt { get; private set; }

        public long? ChangedByUserId { get; private set; }

        public string? ChangedByUserName { get; private set; }

        public string? Reason { get; private set; }

        private ProposalStatusHistory()
        {
        }

        public ProposalStatusHistory(long proposalId, ProposalStatus? fromStatus, ProposalStatus toStatus, long? changedByUserId, string? changedByUserName, string? reason)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(proposalId);

            ProposalId = proposalId;
            FromStatus = fromStatus;
            ToStatus = toStatus;
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
