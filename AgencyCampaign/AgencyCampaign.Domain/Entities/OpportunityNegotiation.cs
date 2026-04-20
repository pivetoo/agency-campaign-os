using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityNegotiation : Entity
    {
        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public decimal Amount { get; private set; }

        public DateTimeOffset NegotiatedAt { get; private set; }

        public string? Notes { get; private set; }

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

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
