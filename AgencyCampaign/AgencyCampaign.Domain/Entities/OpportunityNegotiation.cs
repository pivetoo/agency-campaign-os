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

        public decimal? DiscountPercent { get; private set; }

        public decimal? MarginPercent { get; private set; }

        public int? PaymentTermDays { get; private set; }

        public IReadOnlyCollection<OpportunityApprovalRequest> ApprovalRequests => approvalRequests.AsReadOnly();

        private OpportunityNegotiation()
        {
        }

        public OpportunityNegotiation(long opportunityId, string title, decimal amount, DateTimeOffset negotiatedAt, string? notes = null, decimal? discountPercent = null, decimal? marginPercent = null, int? paymentTermDays = null)
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
            DiscountPercent = ClampPercent(discountPercent);
            MarginPercent = ClampPercent(marginPercent);
            PaymentTermDays = ClampDays(paymentTermDays);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string title, decimal amount, DateTimeOffset negotiatedAt, string? notes, decimal? discountPercent = null, decimal? marginPercent = null, int? paymentTermDays = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            Title = title.Trim();
            Amount = amount;
            NegotiatedAt = negotiatedAt.ToUniversalTime();
            Notes = Normalize(notes);
            DiscountPercent = ClampPercent(discountPercent);
            MarginPercent = ClampPercent(marginPercent);
            PaymentTermDays = ClampDays(paymentTermDays);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkPendingApproval()
        {
            if (Status == OpportunityNegotiationStatus.Approved || Status == OpportunityNegotiationStatus.AcceptedByClient)
            {
                throw new InvalidOperationException("opportunityNegotiation.approved.cannotReturnToPending");
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
                throw new InvalidOperationException("opportunityNegotiation.sendToClient.notApproved");
            }

            Status = OpportunityNegotiationStatus.SentToClient;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAcceptedByClient()
        {
            if (Status != OpportunityNegotiationStatus.SentToClient && Status != OpportunityNegotiationStatus.Approved)
            {
                throw new InvalidOperationException("opportunityNegotiation.clientAcceptance.notApproved");
            }

            Status = OpportunityNegotiationStatus.AcceptedByClient;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal? ClampPercent(decimal? value)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0m) return 0m;
            if (value.Value > 100m) return 100m;
            return value.Value;
        }

        private static int? ClampDays(int? value)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0) return 0;
            if (value.Value > 3650) return 3650;
            return value.Value;
        }
    }
}
