using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityApprovalRequest : Entity
    {
        public long OpportunityNegotiationId { get; private set; }

        public OpportunityNegotiation? OpportunityNegotiation { get; private set; }

        public OpportunityApprovalType ApprovalType { get; private set; }

        public OpportunityApprovalStatus Status { get; private set; } = OpportunityApprovalStatus.Pending;

        public string Reason { get; private set; } = string.Empty;

        public long? RequestedByUserId { get; private set; }

        public string RequestedByUserName { get; private set; } = string.Empty;

        public long? ApprovedByUserId { get; private set; }

        public string? ApprovedByUserName { get; private set; }

        public DateTimeOffset RequestedAt { get; private set; }

        public DateTimeOffset? DecidedAt { get; private set; }

        public string? DecisionNotes { get; private set; }

        private OpportunityApprovalRequest()
        {
        }

        public OpportunityApprovalRequest(long opportunityNegotiationId, OpportunityApprovalType approvalType, string reason, string requestedByUserName, long? requestedByUserId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityNegotiationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);

            OpportunityNegotiationId = opportunityNegotiationId;
            ApprovalType = approvalType;
            Reason = reason.Trim();
            RequestedByUserId = requestedByUserId;
            RequestedByUserName = requestedByUserName.Trim();
            RequestedAt = DateTimeOffset.UtcNow;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Approve(string approvedByUserName, string? decisionNotes = null, long? approvedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(approvedByUserName);

            Status = OpportunityApprovalStatus.Approved;
            ApprovedByUserId = approvedByUserId;
            ApprovedByUserName = approvedByUserName.Trim();
            DecisionNotes = Normalize(decisionNotes);
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject(string approvedByUserName, string? decisionNotes = null, long? approvedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(approvedByUserName);

            Status = OpportunityApprovalStatus.Rejected;
            ApprovedByUserId = approvedByUserId;
            ApprovedByUserName = approvedByUserName.Trim();
            DecisionNotes = Normalize(decisionNotes);
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
