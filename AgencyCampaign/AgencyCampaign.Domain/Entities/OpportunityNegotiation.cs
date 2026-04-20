using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityNegotiation : Entity
    {
        private readonly List<OpportunityApprovalRequest> approvalRequests = [];

        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public decimal Amount { get; private set; }

        public OpportunityNegotiationStatus Status { get; private set; } = OpportunityNegotiationStatus.Draft;

        public DateTimeOffset NegotiatedAt { get; private set; }

        public string? Notes { get; private set; }

        public IReadOnlyCollection<OpportunityApprovalRequest> ApprovalRequests => approvalRequests.AsReadOnly();

        private OpportunityNegotiation()
        {
        }

        public OpportunityNegotiation(long opportunityId, string title, decimal amount, DateTimeOffset negotiatedAt, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            OpportunityId = opportunityId;
            Title = title.Trim();
            Amount = amount;
            Status = OpportunityNegotiationStatus.Draft;
            NegotiatedAt = negotiatedAt.ToUniversalTime();
            Notes = Normalize(notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string title, decimal amount, DateTimeOffset negotiatedAt, string? notes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            Title = title.Trim();
            Amount = amount;
            NegotiatedAt = negotiatedAt.ToUniversalTime();
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkPendingApproval()
        {
            if (Status == OpportunityNegotiationStatus.Approved || Status == OpportunityNegotiationStatus.AcceptedByClient)
            {
                throw new InvalidOperationException("Approved negotiations cannot return to pending approval.");
            }

            Status = OpportunityNegotiationStatus.PendingApproval;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Approve()
        {
            Status = OpportunityNegotiationStatus.Approved;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject()
        {
            Status = OpportunityNegotiationStatus.Rejected;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkSentToClient()
        {
            if (Status != OpportunityNegotiationStatus.Approved)
            {
                throw new InvalidOperationException("Only approved negotiations can be sent to client.");
            }

            Status = OpportunityNegotiationStatus.SentToClient;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAcceptedByClient()
        {
            if (Status != OpportunityNegotiationStatus.SentToClient && Status != OpportunityNegotiationStatus.Approved)
            {
                throw new InvalidOperationException("Negotiation must be approved before client acceptance.");
            }

            Status = OpportunityNegotiationStatus.AcceptedByClient;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
