using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityBoardItemModel
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string BrandName { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public OpportunityStage Stage { get; init; }

        public decimal EstimatedValue { get; init; }

        public DateTimeOffset? ExpectedCloseAt { get; init; }

        public string? InternalOwnerName { get; init; }

        public int ProposalCount { get; init; }

        public int PendingFollowUpsCount { get; init; }

        public int OverdueFollowUpsCount { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
