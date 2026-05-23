namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialOpportunityInsightsModel
    {
        public IReadOnlyCollection<UpcomingClosingItemModel> UpcomingClosings { get; init; } = Array.Empty<UpcomingClosingItemModel>();

        public IReadOnlyCollection<AtRiskItemModel> AtRisk { get; init; } = Array.Empty<AtRiskItemModel>();
    }

    public sealed class UpcomingClosingItemModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? BrandName { get; init; }

        public decimal EstimatedValue { get; init; }

        public decimal Probability { get; init; }

        public DateTimeOffset ExpectedCloseAt { get; init; }

        public int DaysUntilClose { get; init; }

        public string StageName { get; init; } = string.Empty;

        public string? StageColor { get; init; }
    }

    public sealed class AtRiskItemModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? BrandName { get; init; }

        public decimal EstimatedValue { get; init; }

        public string StageName { get; init; } = string.Empty;

        public string? StageColor { get; init; }

        public int DaysInStage { get; init; }
    }
}
