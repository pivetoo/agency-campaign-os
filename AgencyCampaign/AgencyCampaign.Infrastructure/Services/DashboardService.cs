using AgencyCampaign.Application.Models.Dashboard;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly DbContext dbContext;

        public DashboardService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<DashboardChartsModel> GetChartsData(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset startOfYear = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);

            List<CampaignDeliverable> monthlyDeliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.CreatedAt >= startOfYear.AddYears(-1))
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
                .Where(item => item.CreatedAt >= startOfYear.AddYears(-1))
                .ToListAsync(cancellationToken);

            List<Platform> platforms = await dbContext.Set<Platform>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .ToListAsync(cancellationToken);

            IReadOnlyCollection<MonthlyRevenueItem> monthlyRevenue = BuildMonthlyRevenue(monthlyDeliverables, now);
            IReadOnlyCollection<PipelineStageItem> pipeline = BuildPipeline(opportunities);
            IReadOnlyCollection<PlatformDistributionItem> platformDistribution = BuildPlatformDistribution(allDeliverables, platforms);
            IReadOnlyCollection<CreatorGrowthItem> creatorGrowth = BuildCreatorGrowth(creators, now);

            return new DashboardChartsModel
            {
                MonthlyRevenue = monthlyRevenue,
                Pipeline = pipeline,
                PlatformDistribution = platformDistribution,
                CreatorGrowth = creatorGrowth
            };
        }

        private static IReadOnlyCollection<MonthlyRevenueItem> BuildMonthlyRevenue(List<CampaignDeliverable> deliverables, DateTimeOffset now)
        {
            string[] monthNames = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
            int currentMonth = now.Month;

            return Enumerable.Range(1, 12)
                .Select(monthIndex =>
                {
                    int monthOffset = monthIndex - 1;
                    int rawMonth = currentMonth - 11 + monthOffset;
                    int month = ((rawMonth - 1 + 12) % 12) + 1;
                    int year = rawMonth <= 0 ? now.Year - 1 : now.Year;

                    var monthDeliverables = deliverables
                        .Where(item => item.CreatedAt.Month == month && item.CreatedAt.Year == year)
                        .ToList();

                    return new MonthlyRevenueItem
                    {
                        Name = monthNames[month - 1],
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
            string[] monthNames = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
            int currentMonth = now.Month;

            return Enumerable.Range(1, 12)
                .Select(monthIndex =>
                {
                    int monthOffset = monthIndex - 1;
                    int rawMonth = currentMonth - 11 + monthOffset;
                    int month = ((rawMonth - 1 + 12) % 12) + 1;
                    int year = rawMonth <= 0 ? now.Year - 1 : now.Year;

                    int novos = creators.Count(item => item.CreatedAt.Month == month && item.CreatedAt.Year == year);
                    int ativos = creators.Count(item => item.CreatedAt <= new DateTimeOffset(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, TimeSpan.Zero) && item.IsActive);

                    return new CreatorGrowthItem
                    {
                        Name = monthNames[month - 1],
                        Ativos = ativos,
                        Novos = novos
                    };
                })
                .ToArray();
        }
    }
}
