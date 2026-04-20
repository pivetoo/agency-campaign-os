using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityBoardStageModel
    {
        public OpportunityStage Stage { get; init; }

        public int OpportunitiesCount { get; init; }

        public decimal EstimatedValueTotal { get; init; }

        public IReadOnlyCollection<OpportunityBoardItemModel> Items { get; init; } = [];
    }
}
