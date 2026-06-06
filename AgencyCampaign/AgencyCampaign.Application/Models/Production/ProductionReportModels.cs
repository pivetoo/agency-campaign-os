namespace AgencyCampaign.Application.Models.Production
{
    public sealed class CampaignPerformanceLineModel
    {
        public long CampaignId { get; init; }
        public string CampaignName { get; init; } = string.Empty;
        public string? BrandName { get; init; }
        public int Deliverables { get; init; }
        public long TotalReach { get; init; }
        public long TotalImpressions { get; init; }
        public long TotalEngagement { get; init; }
        public decimal? AvgEngagementRate { get; init; }
        public decimal? Emv { get; init; }
    }

    public sealed class CampaignPerformanceModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public IReadOnlyCollection<CampaignPerformanceLineModel> Lines { get; init; } = Array.Empty<CampaignPerformanceLineModel>();
    }

    public sealed class CreatorPerformanceLineModel
    {
        public long CreatorId { get; init; }
        public string CreatorName { get; init; } = string.Empty;
        public int Campaigns { get; init; }
        public int Deliverables { get; init; }
        public long TotalReach { get; init; }
        public long TotalEngagement { get; init; }
        public decimal? AvgEngagementRate { get; init; }
    }

    public sealed class CreatorPerformanceModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public IReadOnlyCollection<CreatorPerformanceLineModel> Lines { get; init; } = Array.Empty<CreatorPerformanceLineModel>();
    }

    public sealed class PlatformProductionLineModel
    {
        public long PlatformId { get; init; }
        public string PlatformName { get; init; } = string.Empty;
        public int Deliverables { get; init; }
        public long TotalReach { get; init; }
        public long TotalImpressions { get; init; }
        public long TotalEngagement { get; init; }
        public decimal? AvgEngagementRate { get; init; }
    }

    public sealed class PlatformProductionModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public IReadOnlyCollection<PlatformProductionLineModel> Lines { get; init; } = Array.Empty<PlatformProductionLineModel>();
    }
}
