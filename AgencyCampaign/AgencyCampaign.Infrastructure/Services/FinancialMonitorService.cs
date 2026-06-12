using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialMonitorService : IFinancialMonitorService
    {
        private const int ProjectionWeeks = 4;
        private const int UpcomingDays = 7;
        private const int DueSoonHours = 48;
        private const int ApprovalStuckHours = 48;
        private const int PeriodCloseGraceDay = 5;

        private readonly DbContext dbContext;
        private readonly IFinancialEntryService financialEntryService;
        private readonly IFinancialReportService financialReportService;

        public FinancialMonitorService(DbContext dbContext, IFinancialEntryService financialEntryService, IFinancialReportService financialReportService)
        {
            this.dbContext = dbContext;
            this.financialEntryService = financialEntryService;
            this.financialReportService = financialReportService;
        }

        public async Task<FinancialMonitorModel> GetMonitor(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset todayStart = new(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset upcomingEnd = todayStart.AddDays(UpcomingDays);

            FinancialSummaryModel receivable = await financialEntryService.GetSummary(FinancialEntryType.Receivable, cancellationToken);
            FinancialSummaryModel payable = await financialEntryService.GetSummary(FinancialEntryType.Payable, cancellationToken);
            CashFlowProjectionModel projection = await financialReportService.GetCashFlowProjection(ProjectionWeeks, cancellationToken);

            List<FinancialEntry> openEntries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => (item.Status == FinancialEntryStatus.Pending || item.Status == FinancialEntryStatus.Overdue) && item.DueAt < upcomingEnd)
                .ToListAsync(cancellationToken);

            List<CreatorPayment> activePayments = await dbContext.Set<CreatorPayment>()
                .AsNoTracking()
                .Where(item => item.Status == PaymentStatus.Pending || item.Status == PaymentStatus.Scheduled || item.Status == PaymentStatus.Failed)
                .ToListAsync(cancellationToken);

            decimal? approvalThreshold = await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => item.CreatorPaymentApprovalThreshold)
                .FirstOrDefaultAsync(cancellationToken);

            List<MonitorReconciliationAccountModel> reconciliation = await BuildReconciliation(cancellationToken);
            MonitorPeriodsModel periods = await BuildPeriods(now, cancellationToken);

            CashFlowProjectionWeekModel? firstNegativeWeek = projection.Series.FirstOrDefault(item => item.ProjectedBalance < 0);

            bool AboveThreshold(CreatorPayment item)
            {
                return approvalThreshold.HasValue && item.NetAmount > approvalThreshold.Value;
            }

            List<CreatorPayment> queue = activePayments.Where(item => item.Status is PaymentStatus.Pending or PaymentStatus.Scheduled).ToList();

            MonitorPulseModel pulse = new()
            {
                RealBalance = projection.OpeningBalance,
                ProjectedBalance30d = projection.Series.Count == 0 ? projection.OpeningBalance : projection.Series.Last().ProjectedBalance,
                ProjectionNegativeAt = firstNegativeWeek?.WeekStart,
                ReceivableOpen = receivable.TotalPending + receivable.TotalOverdue,
                ReceivableOverdue = receivable.TotalOverdue,
                ReceivableOverdueCount = receivable.OverdueCount,
                PayableOpen = payable.TotalPending + payable.TotalOverdue,
                PayableOverdue = payable.TotalOverdue,
                PayableOverdueCount = payable.OverdueCount,
                PayoutQueueCount = queue.Count,
                PayoutQueueAmount = queue.Sum(item => item.NetAmount)
            };

            MonitorPayoutFunnelModel funnel = new()
            {
                PendingApproval = activePayments.Count(item => item.Status == PaymentStatus.Pending && AboveThreshold(item) && !item.IsApproved),
                ReadyToPay = activePayments.Count(item => item.Status == PaymentStatus.Pending && (!AboveThreshold(item) || item.IsApproved)),
                Scheduled = activePayments.Count(item => item.Status == PaymentStatus.Scheduled),
                Failed = activePayments.Count(item => item.Status == PaymentStatus.Failed)
            };

            List<MonitorUpcomingDayModel> upcoming = openEntries
                .Where(item => item.DueAt >= now)
                .GroupBy(item => item.DueAt.UtcDateTime.Date)
                .OrderBy(group => group.Key)
                .Select(group => new MonitorUpcomingDayModel
                {
                    Date = new DateTimeOffset(group.Key, TimeSpan.Zero),
                    InCount = group.Count(item => item.Type == FinancialEntryType.Receivable),
                    InAmount = group.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount),
                    OutCount = group.Count(item => item.Type == FinancialEntryType.Payable),
                    OutAmount = group.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount)
                })
                .ToList();

            List<MonitorAlertModel> alerts = BuildAlerts(now, receivable, payable, firstNegativeWeek, openEntries, activePayments, approvalThreshold, reconciliation, periods);

            return new FinancialMonitorModel
            {
                GeneratedAt = now,
                Pulse = pulse,
                Alerts = alerts,
                Projection = projection,
                Upcoming = upcoming,
                PayoutFunnel = funnel,
                Reconciliation = reconciliation,
                Periods = periods
            };
        }

        private List<MonitorAlertModel> BuildAlerts(DateTimeOffset now, FinancialSummaryModel receivable, FinancialSummaryModel payable, CashFlowProjectionWeekModel? firstNegativeWeek, List<FinancialEntry> openEntries, List<CreatorPayment> activePayments, decimal? approvalThreshold, List<MonitorReconciliationAccountModel> reconciliation, MonitorPeriodsModel periods)
        {
            List<MonitorAlertModel> alerts = [];

            if (firstNegativeWeek is not null)
            {
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.CashGap,
                    Severity = MonitorAlertSeverity.Critical,
                    Count = 1,
                    Amount = firstNegativeWeek.ProjectedBalance,
                    ReferenceDate = firstNegativeWeek.WeekStart
                });
            }

            if (receivable.OverdueCount > 0)
            {
                List<FinancialEntry> overdueReceivables = openEntries.Where(item => item.Type == FinancialEntryType.Receivable && item.DueAt < now).ToList();
                int worstDays = overdueReceivables.Count == 0 ? 0 : (int)Math.Floor((now - overdueReceivables.Min(item => item.DueAt)).TotalDays);
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.OverdueReceivable,
                    Severity = MonitorAlertSeverity.Critical,
                    Count = receivable.OverdueCount,
                    Amount = receivable.TotalOverdue,
                    WorstDays = worstDays
                });
            }

            List<CreatorPayment> failedPayments = activePayments.Where(item => item.Status == PaymentStatus.Failed).ToList();
            if (failedPayments.Count > 0)
            {
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.PaymentFailed,
                    Severity = MonitorAlertSeverity.Critical,
                    Count = failedPayments.Count,
                    Amount = failedPayments.Sum(item => item.NetAmount)
                });
            }

            if (payable.OverdueCount > 0)
            {
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.OverduePayable,
                    Severity = MonitorAlertSeverity.Warning,
                    Count = payable.OverdueCount,
                    Amount = payable.TotalOverdue
                });
            }

            DateTimeOffset dueSoonEnd = now.AddHours(DueSoonHours);
            List<FinancialEntry> dueSoon = openEntries.Where(item => item.DueAt >= now && item.DueAt <= dueSoonEnd).ToList();
            if (dueSoon.Count > 0)
            {
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.DueNext48h,
                    Severity = MonitorAlertSeverity.Warning,
                    Count = dueSoon.Count,
                    AmountIn = dueSoon.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount),
                    AmountOut = dueSoon.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount)
                });
            }

            if (approvalThreshold.HasValue)
            {
                DateTimeOffset stuckBefore = now.AddHours(-ApprovalStuckHours);
                List<CreatorPayment> stuck = activePayments
                    .Where(item => item.Status == PaymentStatus.Pending && item.NetAmount > approvalThreshold.Value && !item.IsApproved && item.CreatedAt <= stuckBefore)
                    .ToList();
                if (stuck.Count > 0)
                {
                    alerts.Add(new MonitorAlertModel
                    {
                        Type = MonitorAlertTypes.ApprovalStuck,
                        Severity = MonitorAlertSeverity.Warning,
                        Count = stuck.Count,
                        Amount = stuck.Sum(item => item.NetAmount)
                    });
                }
            }

            int pendingTransactions = reconciliation.Sum(item => item.Pending);
            if (pendingTransactions > 0)
            {
                MonitorReconciliationAccountModel worst = reconciliation.OrderByDescending(item => item.Pending).First();
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.ReconciliationBacklog,
                    Severity = MonitorAlertSeverity.Warning,
                    Count = pendingTransactions,
                    AccountId = worst.AccountId,
                    AccountName = worst.AccountName
                });
            }

            if (!periods.Previous.IsClosed && now.Day > PeriodCloseGraceDay)
            {
                alerts.Add(new MonitorAlertModel
                {
                    Type = MonitorAlertTypes.PeriodOpen,
                    Severity = MonitorAlertSeverity.Info,
                    Count = 1,
                    ReferenceDate = new DateTimeOffset(periods.Previous.Year, periods.Previous.Month, 1, 0, 0, 0, TimeSpan.Zero)
                });
            }

            return alerts
                .OrderBy(item => item.Severity)
                .ThenByDescending(item => Math.Abs(item.Amount ?? 0))
                .ToList();
        }

        private async Task<List<MonitorReconciliationAccountModel>> BuildReconciliation(CancellationToken cancellationToken)
        {
            var transactionGroups = await dbContext.Set<BankTransaction>()
                .AsNoTracking()
                .GroupBy(item => item.AccountId)
                .Select(group => new
                {
                    AccountId = group.Key,
                    Pending = group.Count(item => item.FinancialEntryId == null),
                    LastImportAt = (DateTimeOffset?)group.Max(item => item.ImportedAt)
                })
                .ToListAsync(cancellationToken);

            if (transactionGroups.Count == 0)
            {
                return [];
            }

            List<long> accountIds = transactionGroups.Select(item => item.AccountId).ToList();
            List<FinancialAccount> accounts = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(item => item.IsActive && accountIds.Contains(item.Id))
                .ToListAsync(cancellationToken);

            return accounts
                .Select(account =>
                {
                    var group = transactionGroups.First(item => item.AccountId == account.Id);
                    return new MonitorReconciliationAccountModel
                    {
                        AccountId = account.Id,
                        AccountName = account.Name,
                        Pending = group.Pending,
                        LastImportAt = group.LastImportAt
                    };
                })
                .OrderByDescending(item => item.Pending)
                .ThenBy(item => item.AccountName)
                .ToList();
        }

        private async Task<MonitorPeriodsModel> BuildPeriods(DateTimeOffset now, CancellationToken cancellationToken)
        {
            DateTimeOffset previousMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-1);
            int currentYear = now.Year;
            int currentMonth = now.Month;
            int previousYear = previousMonth.Year;
            int previousMonthNumber = previousMonth.Month;

            List<FinancialPeriod> rows = await dbContext.Set<FinancialPeriod>()
                .AsNoTracking()
                .Where(item => (item.Year == currentYear && item.Month == currentMonth) || (item.Year == previousYear && item.Month == previousMonthNumber))
                .ToListAsync(cancellationToken);

            FinancialPeriod? current = rows.FirstOrDefault(item => item.Year == currentYear && item.Month == currentMonth);
            FinancialPeriod? previous = rows.FirstOrDefault(item => item.Year == previousYear && item.Month == previousMonthNumber);

            return new MonitorPeriodsModel
            {
                Current = ToPeriodModel(currentYear, currentMonth, current),
                Previous = ToPeriodModel(previousYear, previousMonthNumber, previous)
            };
        }

        private static FinancialPeriodModel ToPeriodModel(int year, int month, FinancialPeriod? row)
        {
            return new FinancialPeriodModel
            {
                Year = year,
                Month = month,
                IsClosed = row?.IsClosed ?? false,
                ClosedAt = row?.ClosedAt,
                ClosedByUserId = row?.ClosedByUserId
            };
        }
    }
}
