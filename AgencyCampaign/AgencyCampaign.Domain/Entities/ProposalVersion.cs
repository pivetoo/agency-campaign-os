using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalVersion : Entity
    {
        public long ProposalId { get; private set; }

        public Proposal? Proposal { get; private set; }

        public int VersionNumber { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public decimal TotalValue { get; private set; }

        public DateTimeOffset? ValidityUntil { get; private set; }

        public string SnapshotJson { get; private set; } = string.Empty;

        public DateTimeOffset SentAt { get; private set; }

        public long? SentByUserId { get; private set; }

        public string? SentByUserName { get; private set; }

        private ProposalVersion()
        {
        }

        public ProposalVersion(long proposalId, int versionNumber, string name, string? description, decimal totalValue, DateTimeOffset? validityUntil, string snapshotJson, long? sentByUserId, string? sentByUserName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(snapshotJson);

            ProposalId = proposalId;
            VersionNumber = versionNumber;
            Name = name.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            TotalValue = totalValue;
            ValidityUntil = validityUntil?.ToUniversalTime();
            SnapshotJson = snapshotJson;
            SentAt = DateTimeOffset.UtcNow;
            SentByUserId = sentByUserId;
            SentByUserName = string.IsNullOrWhiteSpace(sentByUserName) ? null : sentByUserName.Trim();
            CreatedAt = SentAt;
            UpdatedAt = SentAt;
        }
    }
}
