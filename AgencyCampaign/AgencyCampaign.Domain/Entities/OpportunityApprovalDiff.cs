using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityApprovalDiff : Entity
    {
        public long OpportunityApprovalRequestId { get; private set; }

        public OpportunityApprovalRequest? OpportunityApprovalRequest { get; private set; }

        public string Field { get; private set; } = string.Empty;

        public string PolicyValue { get; private set; } = string.Empty;

        public string RequestedValue { get; private set; } = string.Empty;

        public string? Delta { get; private set; }

        public OpportunityApprovalDiffKind Kind { get; private set; }

        public int DisplayOrder { get; private set; }

        private OpportunityApprovalDiff()
        {
        }

        public OpportunityApprovalDiff(long opportunityApprovalRequestId, string field, string policyValue, string requestedValue, OpportunityApprovalDiffKind kind = OpportunityApprovalDiffKind.Change, string? delta = null, int displayOrder = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityApprovalRequestId);
            ArgumentException.ThrowIfNullOrWhiteSpace(field);

            OpportunityApprovalRequestId = opportunityApprovalRequestId;
            Field = field.Trim();
            PolicyValue = (policyValue ?? string.Empty).Trim();
            RequestedValue = (requestedValue ?? string.Empty).Trim();
            Delta = string.IsNullOrWhiteSpace(delta) ? null : delta.Trim();
            Kind = kind;
            DisplayOrder = displayOrder;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
