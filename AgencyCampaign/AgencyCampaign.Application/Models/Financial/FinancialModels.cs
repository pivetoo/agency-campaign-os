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
        public bool IsDefault { get; set; }
        public bool HasEntries { get; set; }
        public long? BankId { get; set; }
        public string? BankCompe { get; set; }
        public string? BankShortName { get; set; }
        public string? BankLogoUrl { get; set; }
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

    public sealed class ReconciliationSummaryModel
    {
        public int Total { get; set; }
        public int Matched { get; set; }
        public int Pending { get; set; }
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

    public sealed class CashFlowProjectionWeekModel
    {
        public DateTimeOffset WeekStart { get; set; }
        public decimal Inflow { get; set; }
        public decimal Outflow { get; set; }
        public decimal Net => Inflow - Outflow;
        public decimal ProjectedBalance { get; set; }
    }

    public sealed class CashFlowProjectionModel
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public int Weeks { get; set; }
        public IReadOnlyCollection<CashFlowProjectionWeekModel> Series { get; set; } = [];
    }

    public sealed class FinancialPeriodModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsClosed { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public long? ClosedByUserId { get; set; }
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

    public sealed class TaxWithholdingLineModel
    {
        public long CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public string? Document { get; set; }
        public TaxRegime? TaxRegime { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal TaxWithheld { get; set; }
        public decimal NetAmount { get; set; }
        public int PaymentCount { get; set; }
    }

    public sealed class TaxWithholdingReportModel
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public IReadOnlyCollection<TaxWithholdingLineModel> Lines { get; set; } = [];
        public decimal TotalGross { get; set; }
        public decimal TotalWithheld { get; set; }
        public decimal TotalNet { get; set; }
    }

    public sealed class CampaignProfitabilityLineModel
    {
        public long CampaignId { get; set; }
        public string? CampaignName { get; set; }
        public decimal Revenue { get; set; }
        public decimal CreatorCost { get; set; }
        public decimal OtherCost { get; set; }
        public decimal Margin { get; set; }
        public decimal MarginPercent { get; set; }
    }

    public sealed class CampaignProfitabilityReportModel
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public IReadOnlyCollection<CampaignProfitabilityLineModel> Lines { get; set; } = [];
        public decimal TotalRevenue { get; set; }
        public decimal TotalCreatorCost { get; set; }
        public decimal TotalOtherCost { get; set; }
        public decimal TotalMargin { get; set; }
    }

    // Resultado por COMPETENCIA: reconhece receita/despesa pela data do fato (OccurredAt), independente
    // do pagamento - separado do fluxo de caixa (regime de caixa), por DP5.
    public sealed class AccrualResultModel
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal Result { get; set; }
    }
}
