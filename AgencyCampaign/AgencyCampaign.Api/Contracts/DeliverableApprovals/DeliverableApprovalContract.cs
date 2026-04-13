using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.DeliverableApprovals
{
    public sealed class DeliverableApprovalContract
    {
        public long Id { get; init; }

        public long CampaignDeliverableId { get; init; }

        public DeliverableApprovalType ApprovalType { get; init; }

        public DeliverableApprovalStatus Status { get; init; }

        public string ReviewerName { get; init; } = string.Empty;

        public string? Comment { get; init; }

        public DateTimeOffset? ApprovedAt { get; init; }

        public DateTimeOffset? RejectedAt { get; init; }

        public DeliverableApprovalDeliverableReferenceContract? CampaignDeliverable { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<DeliverableApproval, DeliverableApprovalContract>> Projection => item => new DeliverableApprovalContract
        {
            Id = item.Id,
            CampaignDeliverableId = item.CampaignDeliverableId,
            ApprovalType = item.ApprovalType,
            Status = item.Status,
            ReviewerName = item.ReviewerName,
            Comment = item.Comment,
            ApprovedAt = item.ApprovedAt,
            RejectedAt = item.RejectedAt,
            CampaignDeliverable = item.CampaignDeliverable == null ? null : new DeliverableApprovalDeliverableReferenceContract
            {
                Id = item.CampaignDeliverable.Id,
                Title = item.CampaignDeliverable.Title
            },
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class DeliverableApprovalDeliverableReferenceContract
    {
        public long Id { get; init; }

        public string Title { get; init; } = string.Empty;
    }
}
