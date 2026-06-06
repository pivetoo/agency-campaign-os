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

    public sealed class DeliverableSlaCampaignLineModel
    {
        public long CampaignId { get; init; }
        public string CampaignName { get; init; } = string.Empty;
        public int Total { get; init; }
        public int PublishedOnTime { get; init; }
        public int PublishedLate { get; init; }
        public int Overdue { get; init; }
        public int Upcoming { get; init; }
    }

    public sealed class DeliverableSlaModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public int PublishedOnTime { get; init; }
        public int PublishedLate { get; init; }
        public int Overdue { get; init; }
        public int Upcoming { get; init; }
        public decimal OnTimeRate { get; init; }
        public IReadOnlyCollection<DeliverableSlaCampaignLineModel> ByCampaign { get; init; } = Array.Empty<DeliverableSlaCampaignLineModel>();
    }

    public sealed class ApprovalCycleModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public int InternalApprovedCount { get; init; }
        public int BrandApprovedCount { get; init; }
        public decimal? AvgInternalApprovalDays { get; init; }
        public decimal? AvgBrandApprovalDays { get; init; }
        public int ContentApprovedCount { get; init; }
        public decimal? AvgRounds { get; init; }
        public decimal? FirstRoundApprovalRate { get; init; }
    }

    public sealed class ContentLicenseReportLineModel
    {
        public long LicenseId { get; init; }
        public long CampaignDeliverableId { get; init; }
        public string DeliverableTitle { get; init; } = string.Empty;
        public string? CampaignName { get; init; }
        public int Type { get; init; }
        public string? Channels { get; init; }
        public DateTimeOffset? StartsAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public int? DaysUntilExpiry { get; init; }
        public int Status { get; init; }
    }

    public sealed class ContentLicenseReportModel
    {
        public DateTimeOffset GeneratedAt { get; init; }
        public int ExpiringSoonDays { get; init; }
        public int ActiveCount { get; init; }
        public int ExpiringSoonCount { get; init; }
        public int ExpiredCount { get; init; }
        public IReadOnlyCollection<ContentLicenseReportLineModel> Lines { get; init; } = Array.Empty<ContentLicenseReportLineModel>();
    }
}
