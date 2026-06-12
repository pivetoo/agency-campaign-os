namespace AgencyCampaign.Application.Models.Financial
{
    public enum MonitorAlertSeverity
    {
        Critical = 1,
        Warning = 2,
        Info = 3
    }

    public static class MonitorAlertTypes
    {
        public const string CashGap = "cash-gap";
        public const string OverdueReceivable = "overdue-receivable";
        public const string PaymentFailed = "payment-failed";
        public const string OverduePayable = "overdue-payable";
        public const string DueNext48h = "due-next-48h";
        public const string ApprovalStuck = "approval-stuck";
        public const string ReconciliationBacklog = "reconciliation-backlog";
        public const string PeriodOpen = "period-open";
    }

    public sealed class MonitorAlertModel
    {
        public string Type { get; set; } = string.Empty;
        public MonitorAlertSeverity Severity { get; set; }
        public int Count { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AmountIn { get; set; }
        public decimal? AmountOut { get; set; }
        public DateTimeOffset? ReferenceDate { get; set; }
        public int? WorstDays { get; set; }
        public long? AccountId { get; set; }
        public string? AccountName { get; set; }
    }

    public sealed class MonitorPulseModel
    {
        public decimal RealBalance { get; set; }
        public decimal ProjectedBalance30d { get; set; }
        public DateTimeOffset? ProjectionNegativeAt { get; set; }
        public decimal ReceivableOpen { get; set; }
        public decimal ReceivableOverdue { get; set; }
        public int ReceivableOverdueCount { get; set; }
        public decimal PayableOpen { get; set; }
        public decimal PayableOverdue { get; set; }
        public int PayableOverdueCount { get; set; }
        public int PayoutQueueCount { get; set; }
        public decimal PayoutQueueAmount { get; set; }
    }

    public sealed class MonitorUpcomingDayModel
    {
        public DateTimeOffset Date { get; set; }
        public int InCount { get; set; }
        public decimal InAmount { get; set; }
        public int OutCount { get; set; }
        public decimal OutAmount { get; set; }
    }

    public sealed class MonitorPayoutFunnelModel
    {
        public int PendingApproval { get; set; }
        public int ReadyToPay { get; set; }
        public int Scheduled { get; set; }
        public int Failed { get; set; }
    }

    public sealed class MonitorReconciliationAccountModel
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int Pending { get; set; }
        public DateTimeOffset? LastImportAt { get; set; }
    }

    public sealed class MonitorPeriodsModel
    {
        public FinancialPeriodModel Current { get; set; } = new();
        public FinancialPeriodModel Previous { get; set; } = new();
    }

    public sealed class FinancialMonitorModel
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public MonitorPulseModel Pulse { get; set; } = new();
        public IReadOnlyCollection<MonitorAlertModel> Alerts { get; set; } = [];
        public CashFlowProjectionModel Projection { get; set; } = new();
        public IReadOnlyCollection<MonitorUpcomingDayModel> Upcoming { get; set; } = [];
        public MonitorPayoutFunnelModel PayoutFunnel { get; set; } = new();
        public IReadOnlyCollection<MonitorReconciliationAccountModel> Reconciliation { get; set; } = [];
        public MonitorPeriodsModel Periods { get; set; } = new();
    }
}
