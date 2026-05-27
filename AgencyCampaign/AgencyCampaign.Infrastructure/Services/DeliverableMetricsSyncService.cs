using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DeliverableMetricsSyncService : IDeliverableMetricsSyncService
    {
        private readonly DbContext dbContext;
        private readonly IApifySocialMetricsClient client;

        public DeliverableMetricsSyncService(DbContext dbContext, IApifySocialMetricsClient client)
        {
            this.dbContext = dbContext;
            this.client = client;
        }

        public async Task<int> SyncCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            if (!client.IsConfigured)
            {
                return 0;
            }

            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .Include(item => item.Platform)
                .Where(item => item.CampaignId == campaignId
                    && item.Status == DeliverableStatus.Published
                    && item.PublishedUrl != null)
                .ToListAsync(cancellationToken);

            int synced = 0;
            foreach (CampaignDeliverable deliverable in deliverables)
            {
                string platformName = deliverable.Platform?.Name ?? string.Empty;
                SocialMetricsResult? result = await client.FetchAsync(platformName, deliverable.PublishedUrl!, cancellationToken);
                if (result is null)
                {
                    continue;
                }

                deliverable.RegisterPublicMetrics(result.Likes, result.Comments, result.Views, result.Shares);
                synced++;
            }

            if (synced > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return synced;
        }
    }
}
