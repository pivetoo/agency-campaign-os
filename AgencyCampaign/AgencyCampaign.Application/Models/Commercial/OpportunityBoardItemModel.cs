namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityBoardItemModel
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string BrandName { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public long CommercialPipelineStageId { get; init; }

        public string CommercialPipelineStageName { get; init; } = string.Empty;

        public string CommercialPipelineStageColor { get; init; } = "#6366f1";

        public decimal EstimatedValue { get; init; }

        public DateTimeOffset? ExpectedCloseAt { get; init; }

        public string? CommercialResponsibleName { get; init; }

        public int ProposalCount { get; init; }

        public int PendingFollowUpsCount { get; init; }

        public int OverdueFollowUpsCount { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public DateTimeOffset? StageEnteredAt { get; init; }

        public int? StageSlaInDays { get; init; }

        public int? DaysInStage { get; init; }

        public string SlaStatus { get; init; } = "ok";
    }
}
