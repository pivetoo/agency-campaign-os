namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityBoardStageModel
    {
        public long CommercialPipelineStageId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";

        public string? Description { get; init; }

        public int DisplayOrder { get; init; }

        public bool IsFinal { get; init; }

        public int FinalBehavior { get; init; }

        public int OpportunitiesCount { get; init; }

        public decimal EstimatedValueTotal { get; init; }

        public IReadOnlyCollection<OpportunityBoardItemModel> Items { get; init; } = [];
    }
}
