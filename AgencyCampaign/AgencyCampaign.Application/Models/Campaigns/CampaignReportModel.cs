namespace AgencyCampaign.Application.Models.Campaigns
{
    public sealed class CampaignReportModel
    {
        public string CampaignName { get; init; } = string.Empty;

        public string? BrandName { get; init; }

        public DateTimeOffset? StartsAt { get; init; }

        public DateTimeOffset? EndsAt { get; init; }

        public CampaignReportTotals Totals { get; init; } = new();

        public IReadOnlyCollection<CampaignReportGroupItem> ByPlatform { get; init; } = [];

        public IReadOnlyCollection<CampaignReportGroupItem> ByCreator { get; init; } = [];

        public IReadOnlyCollection<CampaignReportDeliverableItem> Deliverables { get; init; } = [];
    }

    public sealed class CampaignReportTotals
    {
        public int DeliverablesCount { get; init; }

        public int PublishedCount { get; init; }

        public long TotalReach { get; init; }

        public long TotalImpressions { get; init; }

        public long TotalViews { get; init; }

        public long TotalEngagement { get; init; }

        public decimal? AvgEngagementRate { get; init; }

        public decimal Investment { get; init; }

        public decimal? Cpm { get; init; }

        public decimal? CostPerEngagement { get; init; }
    }

    public sealed class CampaignReportGroupItem
    {
        public string Name { get; init; } = string.Empty;

        public int Deliverables { get; init; }

        public long Reach { get; init; }

        public long Impressions { get; init; }

        public long Engagement { get; init; }
    }

    public sealed class CampaignReportDeliverableItem
    {
        public string Title { get; init; } = string.Empty;

        public string PlatformName { get; init; } = string.Empty;

        public string CreatorName { get; init; } = string.Empty;

        public string? PublishedUrl { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public long? Reach { get; init; }

        public long? Impressions { get; init; }

        public long? Views { get; init; }

        public long? Engagement { get; init; }

        public decimal? EngagementRate { get; init; }
    }

    public sealed class CampaignReportLinkModel
    {
        public string Token { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public DateTimeOffset? RevokedAt { get; init; }

        public DateTimeOffset? LastViewedAt { get; init; }

        public int ViewCount { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
