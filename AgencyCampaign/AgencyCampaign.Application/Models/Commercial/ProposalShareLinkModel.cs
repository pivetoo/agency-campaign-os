namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ProposalShareLinkModel
    {
        public long Id { get; init; }

        public long ProposalId { get; init; }

        public string Token { get; init; } = string.Empty;

        public string PublicUrl { get; init; } = string.Empty;

        public DateTimeOffset? ExpiresAt { get; init; }

        public DateTimeOffset? RevokedAt { get; init; }

        public bool IsActive { get; init; }

        public string? CreatedByUserName { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? LastViewedAt { get; init; }

        public int ViewCount { get; init; }
    }

    public sealed class ProposalPublicViewModel
    {
        public long ProposalId { get; init; }

        public int VersionNumber { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string AgencyName { get; init; } = string.Empty;

        public string BrandName { get; init; } = string.Empty;

        public decimal TotalValue { get; init; }

        public DateTimeOffset? ValidityUntil { get; init; }

        public DateTimeOffset SentAt { get; init; }

        public string SnapshotJson { get; init; } = string.Empty;
    }
}
