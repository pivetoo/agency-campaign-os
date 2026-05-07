namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ProposalVersionModel
    {
        public long Id { get; init; }

        public long ProposalId { get; init; }

        public int VersionNumber { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public decimal TotalValue { get; init; }

        public DateTimeOffset? ValidityUntil { get; init; }

        public DateTimeOffset SentAt { get; init; }

        public long? SentByUserId { get; init; }

        public string? SentByUserName { get; init; }
    }

    public sealed class ProposalVersionDetailModel
    {
        public long Id { get; init; }

        public long ProposalId { get; init; }

        public int VersionNumber { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public decimal TotalValue { get; init; }

        public DateTimeOffset? ValidityUntil { get; init; }

        public string SnapshotJson { get; init; } = string.Empty;

        public DateTimeOffset SentAt { get; init; }

        public string? SentByUserName { get; init; }
    }
}
