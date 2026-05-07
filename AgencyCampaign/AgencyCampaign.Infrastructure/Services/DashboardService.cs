using AgencyCampaign.Application.Models.Dashboard;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private static readonly string[] MonthNames =
            ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

        private readonly DbContext dbContext;

        public DashboardService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<DashboardOverviewModel> GetOverview(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowStart = new DateTimeOffset(now.Year - 1, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            List<CampaignDeliverable> windowDeliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.CreatedAt >= windowStart)
                .ToListAsync(cancellationToken);

            List<CampaignDeliverable> allDeliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<Opportunity> opportunities = await dbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .ToListAsync(cancellationToken);

            List<Creator> creators = await dbContext.Set<Creator>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<Platform> platforms = await dbContext.Set<Platform>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .ToListAsync(cancellationToken);

            List<Campaign> campaigns = await dbContext.Set<Campaign>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            int activeBrands = await dbContext.Set<Brand>()
                .AsNoTracking()
                .CountAsync(item => item.IsActive, cancellationToken);

            List<DeliverableApproval> approvals = await dbContext.Set<DeliverableApproval>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            IReadOnlyCollection<MonthlyRevenueItem> monthlyRevenue = BuildMonthlyRevenue(windowDeliverables, now);
            IReadOnlyCollection<PipelineStageItem> pipeline = BuildPipeline(opportunities);
            IReadOnlyCollection<PlatformDistributionItem> platformDistribution = BuildPlatformDistribution(allDeliverables, platforms);
            IReadOnlyCollection<CreatorGrowthItem> creatorGrowth = BuildCreatorGrowth(creators, now);
            IReadOnlyCollection<OperationHealthItem> operationHealth = BuildOperationHealth(allDeliverables, approvals, opportunities, campaigns);
            HeadlineSummary headline = BuildHeadline(campaigns, activeBrands, creators, allDeliverables, now);

            return new DashboardOverviewModel
            {
                Headline = headline,
                MonthlyRevenue = monthlyRevenue,
                Pipeline = pipeline,
                PlatformDistribution = platformDistribution,
                CreatorGrowth = creatorGrowth,
                OperationHealth = operationHealth
            };
        }

        private static IReadOnlyCollection<MonthlyRevenueItem> BuildMonthlyRevenue(List<CampaignDeliverable> deliverables, DateTimeOffset now)
        {
            return EnumerateLast12Months(now)
                .Select(slot =>
                {
                    var monthDeliverables = deliverables
                        .Where(item => item.CreatedAt.Month == slot.Month && item.CreatedAt.Year == slot.Year)
                        .ToList();

                    return new MonthlyRevenueItem
                    {
                        Name = MonthNames[slot.Month - 1],
                        Receita = monthDeliverables.Sum(item => item.GrossAmount),
                        Fee = monthDeliverables.Sum(item => item.AgencyFeeAmount)
                    };
                })
                .ToArray();
        }

        private static IReadOnlyCollection<PipelineStageItem> BuildPipeline(List<Opportunity> opportunities)
        {
            return opportunities
                .Where(item => item.CommercialPipelineStage != null)
                .GroupBy(item => item.CommercialPipelineStage!.Name)
                .Select(group => new PipelineStageItem
                {
                    Name = group.Key,
                    Oportunidades = group.Count(),
                    Valor = group.Sum(item => item.EstimatedValue)
                })
                .ToArray();
        }

        private static IReadOnlyCollection<PlatformDistributionItem> BuildPlatformDistribution(List<CampaignDeliverable> deliverables, List<Platform> platforms)
        {
            var grouped = deliverables
                .GroupBy(item => item.PlatformId)
                .Select(group => new
                {
                    PlatformId = group.Key,
                    Count = group.Count()
                })
                .ToList();

            var result = platforms
                .Select(platform => new PlatformDistributionItem
                {
                    Name = platform.Name,
                    Value = grouped.FirstOrDefault(item => item.PlatformId == platform.Id)?.Count ?? 0
                })
                .Where(item => item.Value > 0)
                .ToList();

            int otherCount = grouped
                .Where(item => !platforms.Any(p => p.Id == item.PlatformId))
                .Sum(item => item.Count);

            if (otherCount > 0)
            {
                result.Add(new PlatformDistributionItem
                {
                    Name = "Outros",
                    Value = otherCount
                });
            }

            return result;
        }

        private static IReadOnlyCollection<CreatorGrowthItem> BuildCreatorGrowth(List<Creator> creators, DateTimeOffset now)
        {
            return EnumerateLast12Months(now)
                .Select(slot =>
                {
                    DateTimeOffset endOfMonth = new DateTimeOffset(slot.Year, slot.Month, DateTime.DaysInMonth(slot.Year, slot.Month), 23, 59, 59, TimeSpan.Zero);

                    int novos = creators.Count(item => item.CreatedAt.Month == slot.Month && item.CreatedAt.Year == slot.Year);
                    int ativos = creators.Count(item => item.CreatedAt <= endOfMonth && item.IsActive);

                    return new CreatorGrowthItem
                    {
                        Name = MonthNames[slot.Month - 1],
                        Ativos = ativos,
                        Novos = novos
                    };
                })
                .ToArray();
        }

        private static IReadOnlyCollection<OperationHealthItem> BuildOperationHealth(
            List<CampaignDeliverable> deliverables,
            List<DeliverableApproval> approvals,
            List<Opportunity> opportunities,
            List<Campaign> campaigns)
        {
            var publishedDeliverables = deliverables.Where(item => item.PublishedAt.HasValue).ToList();
            decimal onTimeRate = publishedDeliverables.Count > 0
                ? Math.Round(
                    (decimal)publishedDeliverables.Count(item => item.PublishedAt!.Value <= item.DueAt) / publishedDeliverables.Count * 100m,
                    1)
                : 0m;

            var resolvedApprovals = approvals
                .Where(item => item.Status == DeliverableApprovalStatus.Approved || item.Status == DeliverableApprovalStatus.Rejected)
                .ToList();
            decimal approvalRate = resolvedApprovals.Count > 0
                ? Math.Round(
                    (decimal)resolvedApprovals.Count(item => item.Status == DeliverableApprovalStatus.Approved) / resolvedApprovals.Count * 100m,
                    1)
                : 0m;

            decimal totalBudget = campaigns.Where(item => item.IsActive).Sum(item => item.Budget);
            decimal totalFee = deliverables.Sum(item => item.AgencyFeeAmount);
            decimal feeOverBudget = totalBudget > 0
                ? Math.Min(Math.Round(totalFee / totalBudget * 100m, 1), 100m)
                : 0m;

            decimal pipelineRate = opportunities.Count > 0
                ? Math.Round(
                    (decimal)opportunities.Count(item => !item.ClosedAt.HasValue) / opportunities.Count * 100m,
                    1)
                : 0m;

            return
            [
                new OperationHealthItem { Name = "Entregas no prazo", Value = onTimeRate },
                new OperationHealthItem { Name = "Taxa de aprovação", Value = approvalRate },
                new OperationHealthItem { Name = "Fee / Budget", Value = feeOverBudget },
                new OperationHealthItem { Name = "Pipeline ativo", Value = pipelineRate }
            ];
        }

        private static HeadlineSummary BuildHeadline(
            List<Campaign> campaigns,
            int activeBrandsCount,
            List<Creator> creators,
            List<CampaignDeliverable> deliverables,
            DateTimeOffset now)
        {
            int activeCampaigns = campaigns.Count(item =>
                item.IsActive &&
                (item.Status == CampaignStatus.Planned ||
                 item.Status == CampaignStatus.InProgress ||
                 item.Status == CampaignStatus.InReview));

            int activeCreators = creators.Count(item => item.IsActive);

            int pendingDeliverables = deliverables.Count(item =>
                item.Status == DeliverableStatus.Pending || item.Status == DeliverableStatus.InReview);

            decimal monthRevenue = deliverables
                .Where(item => item.CreatedAt.Year == now.Year && item.CreatedAt.Month == now.Month)
                .Sum(item => item.GrossAmount);

            return new HeadlineSummary
            {
                ActiveCampaigns = activeCampaigns,
                ActiveBrands = activeBrandsCount,
                ActiveCreators = activeCreators,
                PendingDeliverables = pendingDeliverables,
                MonthRevenue = monthRevenue
            };
        }

        private static IEnumerable<(int Month, int Year)> EnumerateLast12Months(DateTimeOffset now)
        {
            int currentMonth = now.Month;

            for (int monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                int monthOffset = monthIndex - 1;
                int rawMonth = currentMonth - 11 + monthOffset;
                int month = ((rawMonth - 1 + 12) % 12) + 1;
                int year = rawMonth <= 0 ? now.Year - 1 : now.Year;
                yield return (month, year);
            }
        }
    }
}
