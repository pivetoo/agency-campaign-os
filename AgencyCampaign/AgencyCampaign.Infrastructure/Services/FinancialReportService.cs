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
                ("A vencer", -int.MaxValue, -1),
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

        // Relatorio de retencoes por competencia (PaidAt) para o contador: agrupa por creator os
        // pagamentos pagos com imposto retido, com bruto/retido/liquido, documento e regime tributario.
        public async Task<TaxWithholdingReportModel> GetTaxWithholdingReport(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CreatorPayment> payments = await dbContext.Set<CreatorPayment>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .Where(item => item.Status == PaymentStatus.Paid
                    && item.PaidAt.HasValue
                    && item.PaidAt.Value >= normalizedFrom
                    && item.PaidAt.Value <= normalizedTo
                    && item.TaxWithheld > 0)
                .ToListAsync(cancellationToken);

            TaxWithholdingLineModel[] lines = payments
                .GroupBy(item => item.CreatorId)
                .Select(group => new TaxWithholdingLineModel
                {
                    CreatorId = group.Key,
                    CreatorName = group.First().Creator != null ? group.First().Creator!.Name : null,
                    Document = group.First().Creator != null ? group.First().Creator!.Document : null,
                    TaxRegime = group.First().Creator != null ? group.First().Creator!.TaxRegime : null,
                    GrossAmount = group.Sum(item => item.GrossAmount),
                    TaxWithheld = group.Sum(item => item.TaxWithheld),
                    NetAmount = group.Sum(item => item.NetAmount),
                    PaymentCount = group.Count()
                })
                .OrderByDescending(item => item.TaxWithheld)
                .ToArray();

            return new TaxWithholdingReportModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines,
                TotalGross = lines.Sum(item => item.GrossAmount),
                TotalWithheld = lines.Sum(item => item.TaxWithheld),
                TotalNet = lines.Sum(item => item.NetAmount)
            };
        }

        // Rentabilidade por campanha: cruza receita (recebiveis) x custo (repasses de creator + demais
        // pagaveis) por CampaignId, fechando o ciclo Comercial x Producao x Financeiro. Ignora cancelados.
        public async Task<CampaignProfitabilityReportModel> GetCampaignProfitability(CancellationToken cancellationToken = default)
        {
            List<FinancialEntry> entries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Where(item => item.CampaignId != null && item.Status != FinancialEntryStatus.Cancelled
                    && !item.IsReversed && item.ReversalOfEntryId == null)
                .ToListAsync(cancellationToken);

            CampaignProfitabilityLineModel[] lines = entries
                .GroupBy(item => item.CampaignId!.Value)
                .Select(group =>
                {
                    decimal revenue = group.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount);
                    decimal payable = group.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount);
                    decimal creatorCost = group.Where(item => item.Category == FinancialEntryCategory.CreatorPayout).Sum(item => item.Amount);
                    decimal margin = revenue - payable;
                    return new CampaignProfitabilityLineModel
                    {
                        CampaignId = group.Key,
                        CampaignName = group.First().Campaign != null ? group.First().Campaign!.Name : null,
                        Revenue = revenue,
                        CreatorCost = creatorCost,
                        OtherCost = payable - creatorCost,
                        Margin = margin,
                        MarginPercent = revenue > 0 ? Math.Round(margin / revenue * 100m, 2, MidpointRounding.AwayFromZero) : 0m
                    };
                })
                .OrderByDescending(item => item.Margin)
                .ToArray();

            return new CampaignProfitabilityReportModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                Lines = lines,
                TotalRevenue = lines.Sum(item => item.Revenue),
                TotalCreatorCost = lines.Sum(item => item.CreatorCost),
                TotalOtherCost = lines.Sum(item => item.OtherCost),
                TotalMargin = lines.Sum(item => item.Margin)
            };
        }

        // Resultado por competencia: receita/despesa reconhecidas pela data do fato (OccurredAt) no periodo,
        // independente do status de pagamento - a visao de competencia separada do fluxo de caixa (DP5).
        public async Task<AccrualResultModel> GetAccrualResult(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<FinancialEntry> entries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.Status != FinancialEntryStatus.Cancelled
                    && !item.IsReversed && item.ReversalOfEntryId == null
                    && item.OccurredAt >= normalizedFrom
                    && item.OccurredAt <= normalizedTo)
                .ToListAsync(cancellationToken);

            decimal revenue = entries.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount);
            decimal expense = entries.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount);

            return new AccrualResultModel
            {
                From = normalizedFrom,
                To = normalizedTo,
                Revenue = revenue,
                Expense = expense,
                Result = revenue - expense
            };
        }

        // Projecao de caixa forward-looking (E4): saldo derivado das contas ATIVAS (mesma formula do painel
        // de Contas - InitialBalance + Recebido - Pago) como abertura, e os vencimentos futuros nao pagos
        // (Pending/Overdue) agregados por semana. Vencidos (DueAt no passado) sao dobrados na semana corrente
        // para nao sumirem da visao forward-looking. Read-only, ancorado no Kanvas (ignora saldo do banco).
        public async Task<CashFlowProjectionModel> GetCashFlowProjection(int weeks, CancellationToken cancellationToken = default)
        {
            int horizonWeeks = Math.Clamp(weeks, 1, 52);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset currentWeekStart = StartOfWeek(now);
            DateTimeOffset horizonEnd = currentWeekStart.AddDays(horizonWeeks * 7);

            List<long> activeAccountIds = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);

            decimal initialBalanceTotal = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .SumAsync(item => item.InitialBalance, cancellationToken);

            List<FinancialEntry> paid = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => activeAccountIds.Contains(item.AccountId) && item.Status == FinancialEntryStatus.Paid)
                .ToListAsync(cancellationToken);

            decimal receivedPaid = paid.Where(item => item.Type == FinancialEntryType.Receivable).Sum(item => item.Amount);
            decimal payablePaid = paid.Where(item => item.Type == FinancialEntryType.Payable).Sum(item => item.Amount);
            decimal openingBalance = initialBalanceTotal + receivedPaid - payablePaid;

            List<FinancialEntry> upcoming = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => activeAccountIds.Contains(item.AccountId)
                    && (item.Status == FinancialEntryStatus.Pending || item.Status == FinancialEntryStatus.Overdue)
                    && item.DueAt < horizonEnd)
                .ToListAsync(cancellationToken);

            Dictionary<DateTimeOffset, decimal> inflowByWeek = new();
            Dictionary<DateTimeOffset, decimal> outflowByWeek = new();

            foreach (FinancialEntry entry in upcoming)
            {
                DateTimeOffset entryWeek = StartOfWeek(entry.DueAt);
                DateTimeOffset bucket = entryWeek < currentWeekStart ? currentWeekStart : entryWeek;

                if (entry.Type == FinancialEntryType.Receivable)
                {
                    inflowByWeek.TryGetValue(bucket, out decimal inflow);
                    inflowByWeek[bucket] = inflow + entry.Amount;
                }
                else
                {
                    outflowByWeek.TryGetValue(bucket, out decimal outflow);
                    outflowByWeek[bucket] = outflow + entry.Amount;
                }
            }

            List<CashFlowProjectionWeekModel> series = [];
            decimal running = openingBalance;

            for (int index = 0; index < horizonWeeks; index++)
            {
                DateTimeOffset weekStart = currentWeekStart.AddDays(index * 7);
                inflowByWeek.TryGetValue(weekStart, out decimal inflow);
                outflowByWeek.TryGetValue(weekStart, out decimal outflow);
                running += inflow - outflow;

                series.Add(new CashFlowProjectionWeekModel
                {
                    WeekStart = weekStart,
                    Inflow = inflow,
                    Outflow = outflow,
                    ProjectedBalance = running
                });
            }

            return new CashFlowProjectionModel
            {
                GeneratedAt = now,
                OpeningBalance = openingBalance,
                Weeks = horizonWeeks,
                Series = series
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
