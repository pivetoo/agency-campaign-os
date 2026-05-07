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
