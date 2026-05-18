using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class BankTransaction : Entity
    {
        public long AccountId { get; private set; }

        public string ExternalId { get; private set; } = string.Empty;

        public DateTimeOffset OccurredAt { get; private set; }

        public decimal Amount { get; private set; }

        public BankTransactionDirection Direction { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public string? Category { get; private set; }

        public string? RawPayload { get; private set; }

        public long? FinancialEntryId { get; private set; }

        public DateTimeOffset? MatchedAt { get; private set; }

        public BankTransactionMatchKind? MatchKind { get; private set; }

        public DateTimeOffset ImportedAt { get; private set; }

        private BankTransaction()
        {
        }

        public BankTransaction(long accountId, string externalId, DateTimeOffset occurredAt, decimal amount, BankTransactionDirection direction, string description, string? category = null, string? rawPayload = null)
        {
            if (accountId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(accountId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            AccountId = accountId;
            ExternalId = externalId.Trim();
            OccurredAt = occurredAt;
            Amount = amount;
            Direction = direction;
            Description = description.Trim();
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            RawPayload = string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload;
            ImportedAt = DateTimeOffset.UtcNow;
        }

        public void AttachToEntry(long financialEntryId, BankTransactionMatchKind kind)
        {
            if (financialEntryId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(financialEntryId));
            }

            FinancialEntryId = financialEntryId;
            MatchKind = kind;
            MatchedAt = DateTimeOffset.UtcNow;
        }

        public void DetachFromEntry()
        {
            FinancialEntryId = null;
            MatchKind = null;
            MatchedAt = null;
        }
    }
}
