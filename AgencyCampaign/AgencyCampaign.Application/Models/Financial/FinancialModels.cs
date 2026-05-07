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
}
