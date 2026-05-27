using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorAudienceSyncService : ICreatorAudienceSyncService
    {
        private const string Source = "apify";

        private readonly DbContext dbContext;
        private readonly IApifySocialMetricsClient client;
        private readonly ILogger<CreatorAudienceSyncService> logger;

        public CreatorAudienceSyncService(DbContext dbContext, IApifySocialMetricsClient client, ILogger<CreatorAudienceSyncService> logger)
        {
            this.dbContext = dbContext;
            this.client = client;
            this.logger = logger;
        }

        public async Task<int> SyncCreator(long creatorId, TimeSpan cooldown, CancellationToken cancellationToken = default)
        {
            if (!client.IsConfigured)
            {
                return 0;
            }

            List<CreatorSocialHandle> handles = await EligibleQuery()
                .Where(item => item.CreatorId == creatorId)
                .ToListAsync(cancellationToken);

            return await SyncList(handles, cooldown, cancellationToken);
        }

        public async Task<int> SyncAll(TimeSpan cooldown, CancellationToken cancellationToken = default)
        {
            if (!client.IsConfigured)
            {
                return 0;
            }

            List<CreatorSocialHandle> handles = await EligibleQuery().ToListAsync(cancellationToken);

            return await SyncList(handles, cooldown, cancellationToken);
        }

        private IQueryable<CreatorSocialHandle> EligibleQuery()
        {
            return dbContext.Set<CreatorSocialHandle>()
                .AsTracking()
                .Include(item => item.Platform)
                .Where(item => item.IsActive);
        }

        private async Task<int> SyncList(List<CreatorSocialHandle> handles, TimeSpan cooldown, CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset threshold = now - cooldown;
            int synced = 0;

            foreach (CreatorSocialHandle handle in handles)
            {
                try
                {
                    bool recentlySynced = await dbContext.Set<CreatorSocialHandleSnapshot>()
                        .AnyAsync(item => item.CreatorSocialHandleId == handle.Id && item.CollectedAt > threshold, cancellationToken);
                    if (recentlySynced)
                    {
                        continue;
                    }

                    string platformName = handle.Platform?.Name ?? string.Empty;
                    SocialProfileResult? result = await client.FetchProfileAsync(platformName, handle.Handle, handle.ProfileUrl, cancellationToken);
                    if (result is null || !result.Followers.HasValue)
                    {
                        continue;
                    }

                    handle.SyncAudience(result.Followers, result.EngagementRate);
                    await UpsertSnapshot(handle.Id, now.Year, now.Month, result.Followers, result.EngagementRate, cancellationToken);
                    synced++;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Audience sync failed for handle {HandleId}.", handle.Id);
                }
            }

            if (synced > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return synced;
        }

        private async Task UpsertSnapshot(long handleId, int year, int month, long? followers, decimal? engagementRate, CancellationToken cancellationToken)
        {
            CreatorSocialHandleSnapshot? snapshot = await dbContext.Set<CreatorSocialHandleSnapshot>()
                .FirstOrDefaultAsync(item => item.CreatorSocialHandleId == handleId && item.Year == year && item.Month == month, cancellationToken);

            if (snapshot is null)
            {
                dbContext.Set<CreatorSocialHandleSnapshot>().Add(new CreatorSocialHandleSnapshot(handleId, year, month, followers, engagementRate, Source));
            }
            else
            {
                snapshot.Update(followers, engagementRate, Source);
            }
        }
    }
}
