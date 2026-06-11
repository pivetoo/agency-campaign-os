using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityApprovalReviewer : Entity
    {
        public long OpportunityApprovalRequestId { get; private set; }

        public OpportunityApprovalRequest? OpportunityApprovalRequest { get; private set; }

        public long? UserId { get; private set; }

        public string UserName { get; private set; } = string.Empty;

        public string? Role { get; private set; }

        public bool Required { get; private set; }

        public OpportunityApprovalReviewerStatus Status { get; private set; } = OpportunityApprovalReviewerStatus.Pending;

        public DateTimeOffset? DecidedAt { get; private set; }

        public string? DecisionNotes { get; private set; }

        private OpportunityApprovalReviewer()
        {
        }

        public OpportunityApprovalReviewer(long opportunityApprovalRequestId, string userName, string? role, bool required, long? userId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityApprovalRequestId);
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);

            OpportunityApprovalRequestId = opportunityApprovalRequestId;
            UserId = userId;
            UserName = userName.Trim();
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
            Required = required;
            Status = OpportunityApprovalReviewerStatus.Pending;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        internal OpportunityApprovalReviewer(string userName, string? role, bool required, long? userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);

            UserId = userId;
            UserName = userName.Trim();
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
            Required = required;
            Status = OpportunityApprovalReviewerStatus.Pending;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RecordDecision(OpportunityApprovalReviewerStatus status, string? notes = null)
        {
            if (status == OpportunityApprovalReviewerStatus.Pending)
            {
                throw new InvalidOperationException("opportunityApprovalReviewer.invalidStatus");
            }

            Status = status;
            DecidedAt = DateTimeOffset.UtcNow;
            DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ResetDecision()
        {
            Status = OpportunityApprovalReviewerStatus.Pending;
            DecidedAt = null;
            DecisionNotes = null;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
