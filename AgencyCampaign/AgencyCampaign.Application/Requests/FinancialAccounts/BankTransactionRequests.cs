using System.ComponentModel.DataAnnotations;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.FinancialAccounts
{
    public sealed class ImportBankTransactionsRequest
    {
        [Required]
        public long AccountId { get; set; }

        public decimal? SyncedBalance { get; set; }

        public DateTimeOffset? SyncedAt { get; set; }

        [Required]
        public List<ImportBankTransactionItem> Transactions { get; set; } = new();
    }

    public sealed class MatchBankTransactionRequest
    {
        [Required]
        public long FinancialEntryId { get; set; }
    }

    public sealed class AttachConnectorRequest
    {
        [Required]
        public long ConnectorId { get; set; }
    }

    public sealed class ImportBankTransactionItem
    {
        [Required]
        [StringLength(200)]
        public string ExternalId { get; set; } = string.Empty;

        public DateTimeOffset OccurredAt { get; set; }

        public decimal Amount { get; set; }

        [Required]
        public BankTransactionDirection Direction { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Category { get; set; }

        public string? RawPayload { get; set; }
    }
}
