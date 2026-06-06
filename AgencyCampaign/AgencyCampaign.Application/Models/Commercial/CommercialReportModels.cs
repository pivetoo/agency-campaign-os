namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ProposalsFunnelModel
    {
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public int EmittedCount { get; init; }
        public decimal EmittedValue { get; init; }
        public int AcceptedCount { get; init; }
        public decimal AcceptedValue { get; init; }
        public int RejectedCount { get; init; }
        public decimal AcceptanceRate { get; init; }
    }

    public sealed class BrandRankingLineModel
    {
        public long BrandId { get; init; }
        public string BrandName { get; init; } = string.Empty;
        public int WonCount { get; init; }
        public int LostCount { get; init; }
        public decimal WonValue { get; init; }
        public decimal WinRate { get; init; }
    }

    public sealed class BrandRankingModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public IReadOnlyCollection<BrandRankingLineModel> Lines { get; init; } = Array.Empty<BrandRankingLineModel>();
    }
}
