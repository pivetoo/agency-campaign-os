using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableApproval : Entity
    {
        public long CampaignDeliverableId { get; private set; }

        public CampaignDeliverable? CampaignDeliverable { get; private set; }

        public DeliverableApprovalType ApprovalType { get; private set; }

        public DeliverableApprovalStatus Status { get; private set; } = DeliverableApprovalStatus.Pending;

        public string ReviewerName { get; private set; } = string.Empty;

        public string? Comment { get; private set; }

        public DateTimeOffset? ApprovedAt { get; private set; }

        public DateTimeOffset? RejectedAt { get; private set; }

        private DeliverableApproval()
        {
        }

        public DeliverableApproval(long campaignDeliverableId, DeliverableApprovalType approvalType, string reviewerName, string? comment = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignDeliverableId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reviewerName);

            CampaignDeliverableId = campaignDeliverableId;
            ApprovalType = approvalType;
            ReviewerName = reviewerName.Trim();
            Comment = Normalize(comment);
        }

        public void UpdateReviewer(string reviewerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reviewerName);
            ReviewerName = reviewerName.Trim();
        }

        public void Approve(string? comment = null, DateTimeOffset? approvedAt = null)
        {
            Status = DeliverableApprovalStatus.Approved;
            Comment = Normalize(comment);
            ApprovedAt = approvedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
            RejectedAt = null;
        }

        public void Reject(string? comment = null, DateTimeOffset? rejectedAt = null)
        {
            Status = DeliverableApprovalStatus.Rejected;
            Comment = Normalize(comment);
            RejectedAt = rejectedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
            ApprovedAt = null;
        }

        public void Reset(string? comment = null)
        {
            Status = DeliverableApprovalStatus.Pending;
            Comment = Normalize(comment);
            ApprovedAt = null;
            RejectedAt = null;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
