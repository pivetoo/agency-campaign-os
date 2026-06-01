using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Application.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignReportService : ICampaignReportService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly ITenantContext tenantContext;

        public CampaignReportService(DbContext dbContext, ICurrentUser currentUser, ITenantContext tenantContext)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.tenantContext = tenantContext;
        }

        public async Task<CampaignReportLinkModel> CreateOrGetLink(long campaignId, CancellationToken cancellationToken = default)
        {
            bool campaignExists = await dbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            CampaignReportLink? link = await dbContext.Set<CampaignReportLink>()
                .Where(item => item.CampaignId == campaignId && item.RevokedAt == null)
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (link is null)
            {
                string token = PublicLinkToken.Compose(tenantContext.TenantId, GenerateToken());
                link = new CampaignReportLink(campaignId, token, currentUser.UserId, currentUser.UserName);
                dbContext.Set<CampaignReportLink>().Add(link);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ToModel(link);
        }

        public async Task<CampaignReportModel?> GetReportByToken(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            CampaignReportLink? link = await dbContext.Set<CampaignReportLink>()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (link is null || !link.IsActive())
            {
                return null;
            }

            link.RegisterView();
            await dbContext.SaveChangesAsync(cancellationToken);

            return await BuildReport(link.CampaignId, cancellationToken);
        }

        private async Task<CampaignReportModel?> BuildReport(long campaignId, CancellationToken cancellationToken)
        {
            Campaign? campaign = await dbContext.Set<Campaign>()
                .AsNoTracking()
                .Include(item => item.Brand)
                .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);

            if (campaign is null)
            {
                return null;
            }

            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Platform)
                .Include(item => item.CampaignCreator)
                    .ThenInclude(campaignCreator => campaignCreator!.Creator)
                .Where(item => item.CampaignId == campaignId)
                .ToListAsync(cancellationToken);

            static long Engagement(CampaignDeliverable item) => (item.Likes ?? 0) + (item.Comments ?? 0) + (item.Shares ?? 0) + (item.Saves ?? 0);
            static string PlatformName(CampaignDeliverable item) => item.Platform?.Name ?? "-";
            static string CreatorName(CampaignDeliverable item) => item.CampaignCreator?.Creator?.StageName ?? item.CampaignCreator?.Creator?.Name ?? "-";

            long totalReach = deliverables.Sum(item => item.Reach ?? 0);
            long totalImpressions = deliverables.Sum(item => item.Impressions ?? 0);
            long totalViews = deliverables.Sum(item => item.Views ?? 0);
            long totalEngagement = deliverables.Sum(Engagement);

            List<decimal> rates = deliverables.Where(item => item.EngagementRate.HasValue).Select(item => item.EngagementRate!.Value).ToList();
            decimal? avgRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

            decimal investment = campaign.Budget;
            decimal? cpm = totalReach > 0 ? Math.Round(investment / totalReach * 1000m, 2) : null;
            decimal? costPerEngagement = totalEngagement > 0 ? Math.Round(investment / totalEngagement, 2) : null;

            decimal? emvRate = await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .Select(item => item.EmvCpmRate)
                .FirstOrDefaultAsync(cancellationToken);
            long emvBase = totalImpressions > 0 ? totalImpressions : totalReach;
            decimal? emv = emvRate.HasValue && emvRate.Value > 0 && emvBase > 0
                ? Math.Round(emvBase / 1000m * emvRate.Value, 2)
                : null;

            List<CampaignCreator> reportCreators = await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => item.CampaignId == campaignId)
                .ToListAsync(cancellationToken);
            bool hasAttribution = reportCreators.Any(item => item.AttributedRevenue.HasValue || item.AttributedOrders.HasValue);
            decimal? attributedRevenue = hasAttribution ? reportCreators.Sum(item => item.AttributedRevenue ?? 0) : null;
            int? attributedOrders = hasAttribution ? reportCreators.Sum(item => item.AttributedOrders ?? 0) : null;
            decimal? roi = attributedRevenue.HasValue && attributedRevenue.Value > 0 && investment > 0
                ? Math.Round(attributedRevenue.Value / investment, 2)
                : null;

            CampaignReportGroupItem[] byPlatform = deliverables
                .GroupBy(PlatformName)
                .Select(group => new CampaignReportGroupItem
                {
                    Name = group.Key,
                    Deliverables = group.Count(),
                    Reach = group.Sum(item => item.Reach ?? 0),
                    Impressions = group.Sum(item => item.Impressions ?? 0),
                    Engagement = group.Sum(Engagement)
                })
                .OrderByDescending(item => item.Reach)
                .ToArray();

            CampaignReportGroupItem[] byCreator = deliverables
                .GroupBy(CreatorName)
                .Select(group => new CampaignReportGroupItem
                {
                    Name = group.Key,
                    Deliverables = group.Count(),
                    Reach = group.Sum(item => item.Reach ?? 0),
                    Impressions = group.Sum(item => item.Impressions ?? 0),
                    Engagement = group.Sum(Engagement)
                })
                .OrderByDescending(item => item.Reach)
                .ToArray();

            CampaignReportDeliverableItem[] items = deliverables
                .OrderByDescending(item => item.PublishedAt ?? DateTimeOffset.MinValue)
                .Select(item => new CampaignReportDeliverableItem
                {
                    Title = item.Title,
                    PlatformName = PlatformName(item),
                    CreatorName = CreatorName(item),
                    PublishedUrl = item.PublishedUrl,
                    PublishedAt = item.PublishedAt,
                    Reach = item.Reach,
                    Impressions = item.Impressions,
                    Views = item.Views,
                    Engagement = item.MetricsSource == DeliverableMetricsSource.None ? null : Engagement(item),
                    EngagementRate = item.EngagementRate
                })
                .ToArray();

            return new CampaignReportModel
            {
                CampaignName = campaign.Name,
                BrandName = campaign.Brand?.Name,
                StartsAt = campaign.StartsAt,
                EndsAt = campaign.EndsAt,
                Totals = new CampaignReportTotals
                {
                    DeliverablesCount = deliverables.Count,
                    PublishedCount = deliverables.Count(item => item.Status == DeliverableStatus.Published),
                    TotalReach = totalReach,
                    TotalImpressions = totalImpressions,
                    TotalViews = totalViews,
                    TotalEngagement = totalEngagement,
                    AvgEngagementRate = avgRate,
                    Investment = investment,
                    Cpm = cpm,
                    CostPerEngagement = costPerEngagement,
                    Emv = emv,
                    AttributedRevenue = attributedRevenue,
                    AttributedOrders = attributedOrders,
                    Roi = roi
                },
                ByPlatform = byPlatform,
                ByCreator = byCreator,
                Deliverables = items
            };
        }

        private static CampaignReportLinkModel ToModel(CampaignReportLink link)
        {
            return new CampaignReportLinkModel
            {
                Token = link.Token,
                IsActive = link.IsActive(),
                RevokedAt = link.RevokedAt,
                LastViewedAt = link.LastViewedAt,
                ViewCount = link.ViewCount,
                CreatedAt = link.CreatedAt
            };
        }

        private static string GenerateToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
