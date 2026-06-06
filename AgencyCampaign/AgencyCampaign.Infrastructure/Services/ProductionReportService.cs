using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProductionReportService : IProductionReportService
    {
        private readonly DbContext dbContext;

        public ProductionReportService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CampaignPerformanceModel> GetCampaignPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            decimal? emvRate = await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .Select(item => item.EmvCpmRate)
                .FirstOrDefaultAsync(cancellationToken);

            CampaignPerformanceLineModel[] lines = deliverables
                .GroupBy(d => d.CampaignId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalImpressions = group.Sum(d => d.Impressions ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    long emvBase = totalImpressions > 0 ? totalImpressions : totalReach;
                    decimal? emv = emvRate.HasValue && emvRate.Value > 0 && emvBase > 0
                        ? Math.Round(emvBase / 1000m * emvRate.Value, 2)
                        : null;

                    CampaignDeliverable first = group.First();
                    return new CampaignPerformanceLineModel
                    {
                        CampaignId = group.Key,
                        CampaignName = first.Campaign?.Name ?? string.Empty,
                        BrandName = first.Campaign?.Brand?.Name,
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalImpressions = totalImpressions,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate,
                        Emv = emv
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .Take(50)
                .ToArray();

            return new CampaignPerformanceModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        public async Task<CreatorPerformanceModel> GetCreatorPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            CreatorPerformanceLineModel[] lines = deliverables
                .GroupBy(d => d.CampaignCreator!.CreatorId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    CampaignDeliverable first = group.First();
                    string creatorName = first.CampaignCreator?.Creator?.StageName
                        ?? first.CampaignCreator?.Creator?.Name
                        ?? string.Empty;

                    return new CreatorPerformanceLineModel
                    {
                        CreatorId = group.Key,
                        CreatorName = creatorName,
                        Campaigns = group.Select(d => d.CampaignId).Distinct().Count(),
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .Take(50)
                .ToArray();

            return new CreatorPerformanceModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        public async Task<PlatformProductionModel> GetPlatformProduction(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            PlatformProductionLineModel[] lines = deliverables
                .GroupBy(d => d.PlatformId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalImpressions = group.Sum(d => d.Impressions ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    CampaignDeliverable first = group.First();
                    return new PlatformProductionLineModel
                    {
                        PlatformId = group.Key,
                        PlatformName = first.Platform?.Name ?? string.Empty,
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalImpressions = totalImpressions,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .ToArray();

            return new PlatformProductionModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        private async Task<List<CampaignDeliverable>> LoadPublished(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            return await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(d => d.Campaign).ThenInclude(c => c!.Brand)
                .Include(d => d.Platform)
                .Include(d => d.CampaignCreator).ThenInclude(cc => cc!.Creator)
                .Where(d => d.PublishedAt != null && d.PublishedAt >= from && d.PublishedAt < to)
                .ToListAsync(ct);
        }

        private static long Engagement(CampaignDeliverable d)
        {
            return (d.Likes ?? 0) + (d.Comments ?? 0) + (d.Shares ?? 0) + (d.Saves ?? 0);
        }
    }
}
