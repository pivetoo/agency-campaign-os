namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialAnalyticsModel
    {
        public DateTimeOffset PeriodStart { get; init; }

        public DateTimeOffset PeriodEnd { get; init; }

        public long? UserId { get; init; }

        public int ClosedCount { get; init; }

        public int WonCount { get; init; }

        public int LostCount { get; init; }

        public decimal WinRate { get; init; }

        public decimal AverageCycleDays { get; init; }

        public IReadOnlyCollection<StageConversionModel> ConversionByStage { get; init; } = Array.Empty<StageConversionModel>();

        public IReadOnlyCollection<StageTimeModel> AverageTimeInStage { get; init; } = Array.Empty<StageTimeModel>();

        public IReadOnlyCollection<ReasonAggregateModel> WinReasons { get; init; } = Array.Empty<ReasonAggregateModel>();

        public IReadOnlyCollection<ReasonAggregateModel> LossReasons { get; init; } = Array.Empty<ReasonAggregateModel>();

        public IReadOnlyCollection<PerformerModel> TopPerformers { get; init; } = Array.Empty<PerformerModel>();
    }

    public sealed class StageConversionModel
    {
        public long StageId { get; init; }

        public string StageName { get; init; } = string.Empty;

        public string? StageColor { get; init; }

        public int DisplayOrder { get; init; }

        public int Entered { get; init; }

        public int Advanced { get; init; }

        public int Stuck { get; init; }

        public int Lost { get; init; }

        public decimal ConversionRate { get; init; }
    }

    public sealed class StageTimeModel
    {
        public long StageId { get; init; }

        public string StageName { get; init; } = string.Empty;

        public string? StageColor { get; init; }

        public int DisplayOrder { get; init; }

        public decimal AverageDays { get; init; }

        public int Samples { get; init; }
    }

    public sealed class ReasonAggregateModel
    {
        public long? ReasonId { get; init; }

        public string ReasonName { get; init; } = string.Empty;

        public string? ReasonColor { get; init; }

        public int Count { get; init; }

        public decimal TotalValue { get; init; }
    }

    public sealed class PerformerModel
    {
        public long? UserId { get; init; }

        public string UserName { get; init; } = string.Empty;

        public int WonCount { get; init; }

        public decimal WonTotal { get; init; }
    }
}
