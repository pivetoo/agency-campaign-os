using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialReportService : IFinancialReportService
    {
        private readonly DbContext dbContext;

        public FinancialReportService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CashFlowSeriesModel> GetCashFlow(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<FinancialEntry> entries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.Status != FinancialEntryStatus.Cancelled)
                .Where(item => (item.DueAt >= normalizedFrom && item.DueAt <= normalizedTo) ||
                               (item.PaidAt.HasValue && item.PaidAt.Value >= normalizedFrom && item.PaidAt.Value <= normalizedTo))
                .ToListAsync(cancellationToken);

            var pendingPoints = entries
                .Where(item => item.Status != FinancialEntryStatus.Paid)
                .GroupBy(item => BucketDate(item.DueAt, granularity))
                .Select(group => new CashFlowPointModel
                {
                    Bucket = group.Key,
                    Inflow = group.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount),
                    Outflow = group.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount)
                })
                .OrderBy(item => item.Bucket)
                .ToArray();

            var settledPoints = entries
                .Where(item => item.Status == FinancialEntryStatus.Paid && item.PaidAt.HasValue)
                .GroupBy(item => BucketDate(item.PaidAt!.Value, granularity))
                .Select(group => new CashFlowPointModel
                {
                    Bucket = group.Key,
                    Inflow = group.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount),
                    Outflow = group.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount)
                })
                .OrderBy(item => item.Bucket)
                .ToArray();

            return new CashFlowSeriesModel
            {
                From = normalizedFrom,
                To = normalizedTo,
                Granularity = granularity,
                Pending = pendingPoints,
                Settled = settledPoints
            };
        }

        public async Task<AgingReportModel> GetAgingReport(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<FinancialEntry> entries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.Status == FinancialEntryStatus.Pending || item.Status == FinancialEntryStatus.Overdue)
                .ToListAsync(cancellationToken);

            (string label, int min, int? max)[] ranges =
            [
                ("A vencer", -int.MaxValue, 0),
                ("0-30 dias", 0, 30),
                ("31-60 dias", 31, 60),
                ("61-90 dias", 61, 90),
                ("90+ dias", 91, null)
            ];

            var buckets = ranges.Select(range =>
            {
                IEnumerable<FinancialEntry> filtered = entries.Where(item =>
                {
                    int daysOverdue = (int)Math.Floor((now - item.DueAt).TotalDays);
                    bool aboveMin = range.min == -int.MaxValue || daysOverdue >= range.min;
                    bool belowMax = !range.max.HasValue || daysOverdue <= range.max.Value;
                    return aboveMin && belowMax;
                });

                var receivable = filtered.Where(item => item.Type == FinancialEntryType.Receivable).ToArray();
                var payable = filtered.Where(item => item.Type == FinancialEntryType.Payable).ToArray();

                return new AgingBucketModel
                {
                    Label = range.label,
                    MinDays = range.min == -int.MaxValue ? 0 : range.min,
                    MaxDays = range.max,
                    TotalReceivable = receivable.Sum(item => item.Amount),
                    ReceivableCount = receivable.Length,
                    TotalPayable = payable.Sum(item => item.Amount),
                    PayableCount = payable.Length
                };
            }).ToArray();

            return new AgingReportModel
            {
                GeneratedAt = now,
                Buckets = buckets
            };
        }

        private static DateTimeOffset BucketDate(DateTimeOffset value, CashFlowGranularity granularity)
        {
            DateTimeOffset utc = value.ToUniversalTime();
            return granularity switch
            {
                CashFlowGranularity.Month => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
                CashFlowGranularity.Week => StartOfWeek(utc),
                _ => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            };
        }

        private static DateTimeOffset StartOfWeek(DateTimeOffset value)
        {
            int diff = (7 + (int)value.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            DateTimeOffset start = value.AddDays(-diff);
            return new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
