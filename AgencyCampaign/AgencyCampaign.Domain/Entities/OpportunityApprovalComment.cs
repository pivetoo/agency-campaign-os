using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityApprovalComment : Entity
    {
        public long OpportunityApprovalRequestId { get; private set; }

        public OpportunityApprovalRequest? OpportunityApprovalRequest { get; private set; }

        public long? UserId { get; private set; }

        public string UserName { get; private set; } = string.Empty;

        public string Role { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        private OpportunityApprovalComment()
        {
        }

        public OpportunityApprovalComment(long opportunityApprovalRequestId, string userName, string body, string role = "observador", long? userId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityApprovalRequestId);
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);

            OpportunityApprovalRequestId = opportunityApprovalRequestId;
            UserId = userId;
            UserName = userName.Trim();
            Role = string.IsNullOrWhiteSpace(role) ? "observador" : role.Trim().ToLowerInvariant();
            Body = body.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Edit(string body)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            Body = body.Trim();
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
