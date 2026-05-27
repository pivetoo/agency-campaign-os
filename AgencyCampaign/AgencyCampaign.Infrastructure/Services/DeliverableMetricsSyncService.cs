using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DeliverableMetricsSyncService : IDeliverableMetricsSyncService
    {
        private readonly DbContext dbContext;
        private readonly IApifySocialMetricsClient client;
        private readonly ILogger<DeliverableMetricsSyncService> logger;

        public DeliverableMetricsSyncService(DbContext dbContext, IApifySocialMetricsClient client, ILogger<DeliverableMetricsSyncService> logger)
        {
            this.dbContext = dbContext;
            this.client = client;
            this.logger = logger;
        }

        public async Task<int> SyncCampaign(long campaignId, TimeSpan cooldown, CancellationToken cancellationToken = default)
        {
            if (!client.IsConfigured)
            {
                return 0;
            }

            List<CampaignDeliverable> deliverables = await EligibleQuery()
                .Where(item => item.CampaignId == campaignId)
                .ToListAsync(cancellationToken);

            return await SyncList(deliverables, cooldown, cancellationToken);
        }

        public async Task<int> SyncAll(TimeSpan cooldown, CancellationToken cancellationToken = default)
        {
            if (!client.IsConfigured)
            {
                return 0;
            }

            List<CampaignDeliverable> deliverables = await EligibleQuery().ToListAsync(cancellationToken);

            return await SyncList(deliverables, cooldown, cancellationToken);
        }

        private IQueryable<CampaignDeliverable> EligibleQuery()
        {
            return dbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .Include(item => item.Platform)
                .Where(item => item.Status == DeliverableStatus.Published && item.PublishedUrl != null);
        }

        private async Task<int> SyncList(List<CampaignDeliverable> deliverables, TimeSpan cooldown, CancellationToken cancellationToken)
        {
            DateTimeOffset threshold = DateTimeOffset.UtcNow - cooldown;
            int synced = 0;

            foreach (CampaignDeliverable deliverable in deliverables)
            {
                if (deliverable.MetricsCollectedAt.HasValue && deliverable.MetricsCollectedAt.Value > threshold)
                {
                    continue;
                }

                try
                {
                    string platformIdentifier = deliverable.Platform?.Identifier ?? string.Empty;
                    SocialMetricsResult? result = await client.FetchAsync(platformIdentifier, deliverable.PublishedUrl!, cancellationToken);
                    if (result is null)
                    {
                        continue;
                    }

                    deliverable.RegisterPublicMetrics(result.Likes, result.Comments, result.Views, result.Shares);
                    synced++;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Post metrics sync failed for deliverable {DeliverableId}.", deliverable.Id);
                }
            }

            if (synced > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return synced;
        }
    }
}
