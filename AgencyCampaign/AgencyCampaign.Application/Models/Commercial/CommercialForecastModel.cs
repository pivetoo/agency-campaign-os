namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialForecastModel
    {
        public IReadOnlyCollection<CommercialForecastMonthModel> Months { get; init; } = [];

        public decimal TotalEstimated { get; init; }

        public decimal TotalWeighted { get; init; }

        public int TotalCount { get; init; }
    }

    public sealed class CommercialForecastMonthModel
    {
        public string Month { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public decimal Estimated { get; init; }

        public decimal Weighted { get; init; }

        public int Count { get; init; }
    }
}
