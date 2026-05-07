namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialFunnelStageModel
    {
        public long StageId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";

        public int DisplayOrder { get; init; }

        public int IsFinalBehavior { get; init; }

        public int OpenCount { get; init; }

        public decimal OpenValue { get; init; }

        public int EnteredCount { get; init; }

        public decimal ConversionRate { get; init; }
    }
}
