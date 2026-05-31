namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialForecastModel
    {
        public DateTimeOffset PeriodStart { get; init; }

        public DateTimeOffset PeriodEnd { get; init; }

        public long? UserId { get; init; }

        public decimal WeightedTotal { get; init; }

        public decimal UnweightedTotal { get; init; }

        public decimal WonTotal { get; init; }

        public decimal LostTotal { get; init; }

        public int OpenCount { get; init; }

        public int WonCount { get; init; }

        public int LostCount { get; init; }

        public int NoDateCount { get; init; }

        public decimal NoDateTotal { get; init; }

        public IReadOnlyCollection<CommercialForecastStageBreakdown> ByStage { get; init; } = Array.Empty<CommercialForecastStageBreakdown>();
    }

    public sealed class CommercialForecastStageBreakdown
    {
        public long StageId { get; init; }

        public string StageName { get; init; } = string.Empty;

        public string? StageColor { get; init; }

        public int Count { get; init; }

        public decimal TotalValue { get; init; }

        public decimal WeightedValue { get; init; }

        public decimal AverageProbability { get; init; }
    }
}
