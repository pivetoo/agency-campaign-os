using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.Financial
{
    public sealed class FinancialAccountModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public FinancialAccountType Type { get; set; }
        public string? Bank { get; set; }
        public string? Agency { get; set; }
        public string? Number { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Color { get; set; } = "#6366f1";
        public bool IsActive { get; set; }
        public long? IntegrationConnectorId { get; set; }
        public decimal? LastSyncedBalance { get; set; }
        public DateTimeOffset? LastSyncedAt { get; set; }
        public FinancialAccountSyncStatus SyncStatus { get; set; }
    }

    public sealed class BankTransactionModel
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public DateTimeOffset OccurredAt { get; set; }
        public decimal Amount { get; set; }
        public BankTransactionDirection Direction { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public long? FinancialEntryId { get; set; }
        public DateTimeOffset? MatchedAt { get; set; }
        public BankTransactionMatchKind? MatchKind { get; set; }
        public DateTimeOffset ImportedAt { get; set; }
    }

    public sealed class ImportBankTransactionsResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int AutoMatched { get; set; }
    }

    public sealed class FinancialAccountSummaryModel
    {
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public decimal TotalKanvasBalance { get; set; }
        public decimal TotalLastSyncedBalance { get; set; }
        public int SyncedAccountsCount { get; set; }
        public int PendingSyncAccountsCount { get; set; }
        public int ErroredSyncAccountsCount { get; set; }
    }

    public sealed class FinancialSummaryModel
    {
        public FinancialEntryType Type { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalSettledThisMonth { get; set; }
        public decimal TotalOverdue { get; set; }
        public decimal TotalDueNext7Days { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public enum CashFlowGranularity
    {
        Day = 0,
        Week = 1,
        Month = 2
    }

    public sealed class CashFlowPointModel
    {
        public DateTimeOffset Bucket { get; set; }
        public decimal Inflow { get; set; }
        public decimal Outflow { get; set; }
        public decimal Net => Inflow - Outflow;
    }

    public sealed class CashFlowSeriesModel
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public CashFlowGranularity Granularity { get; set; }
        public IReadOnlyCollection<CashFlowPointModel> Pending { get; set; } = [];
        public IReadOnlyCollection<CashFlowPointModel> Settled { get; set; } = [];
    }

    public sealed class AgingBucketModel
    {
        public string Label { get; set; } = string.Empty;
        public int MinDays { get; set; }
        public int? MaxDays { get; set; }
        public decimal TotalReceivable { get; set; }
        public int ReceivableCount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PayableCount { get; set; }
    }

    public sealed class AgingReportModel
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public IReadOnlyCollection<AgingBucketModel> Buckets { get; set; } = [];
    }
}
