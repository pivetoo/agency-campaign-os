namespace AgencyCampaign.Application.Models.Deliverables
{
    public sealed class DeliverableShareLinkModel
    {
        public long Id { get; set; }
        public long CampaignDeliverableId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public DateTimeOffset? LastViewedAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class DeliverablePublicViewModel
    {
        public long DeliverableId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CreatorName { get; set; }
        public string? PlatformName { get; set; }
        public string? DeliverableKindName { get; set; }
        public string? CampaignName { get; set; }
        public string? BrandName { get; set; }
        public DateTimeOffset DueAt { get; set; }
        public string? PublishedUrl { get; set; }
        public string? EvidenceUrl { get; set; }
        public int Status { get; set; }
        public int? ApprovalStatus { get; set; }
        public string? ApprovalComment { get; set; }
    }

    public sealed class DeliverableApprovalModel
    {
        public long Id { get; set; }
        public long CampaignDeliverableId { get; set; }
        public int ApprovalType { get; set; }
        public int Status { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset? RejectedAt { get; set; }
    }

    public sealed class PendingApprovalModel
    {
        public long DeliverableId { get; set; }
        public string DeliverableTitle { get; set; } = string.Empty;
        public string? CampaignName { get; set; }
        public string? BrandName { get; set; }
        public string? CreatorName { get; set; }
        public string? PlatformName { get; set; }
        public DateTimeOffset DueAt { get; set; }
        public int DeliverableStatus { get; set; }
        public IReadOnlyCollection<DeliverableApprovalModel> Approvals { get; set; } = [];
        public bool HasActiveShareLink { get; set; }
    }
}
