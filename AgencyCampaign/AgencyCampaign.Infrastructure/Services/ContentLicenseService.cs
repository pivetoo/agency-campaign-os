using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.ContentLicenses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Options;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ContentLicenseService : IContentLicenseService
    {
        private readonly DbContext dbContext;
        private readonly INotificationService notificationService;
        private readonly ContentLicenseOptions options;

        public ContentLicenseService(DbContext dbContext, INotificationService notificationService, IOptions<ContentLicenseOptions> options)
        {
            this.dbContext = dbContext;
            this.notificationService = notificationService;
            this.options = options.Value;
        }

        public async Task<IReadOnlyList<ContentLicenseModel>> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default)
        {
            List<DeliverableContentLicense> licenses = await dbContext.Set<DeliverableContentLicense>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == deliverableId)
                .OrderBy(item => item.ExpiresAt)
                .ToListAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            return licenses.Select(item => ToModel(item, now)).ToList();
        }

        public async Task<ContentLicenseModel> Add(long deliverableId, AddContentLicenseRequest request, CancellationToken cancellationToken = default)
        {
            DeliverableContentLicense license = new(deliverableId, request.Type, request.Channels, request.StartsAt, request.ExpiresAt, request.Value, request.Notes, request.CampaignDocumentId);
            dbContext.Set<DeliverableContentLicense>().Add(license);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToModel(license, DateTimeOffset.UtcNow);
        }

        public async Task<ContentLicenseModel> Update(long licenseId, UpdateContentLicenseRequest request, CancellationToken cancellationToken = default)
        {
            DeliverableContentLicense license = await Load(licenseId, cancellationToken);
            license.Update(request.Type, request.Channels, request.StartsAt, request.ExpiresAt, request.Value, request.Notes, request.CampaignDocumentId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToModel(license, DateTimeOffset.UtcNow);
        }

        public async Task Delete(long licenseId, CancellationToken cancellationToken = default)
        {
            DeliverableContentLicense license = await Load(licenseId, cancellationToken);
            dbContext.Set<DeliverableContentLicense>().Remove(license);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> ApplyToCampaign(long licenseId, CancellationToken cancellationToken = default)
        {
            DeliverableContentLicense source = await Load(licenseId, cancellationToken);

            long? campaignId = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.Id == source.CampaignDeliverableId)
                .Select(item => (long?)item.CampaignId)
                .FirstOrDefaultAsync(cancellationToken);

            if (campaignId is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            List<long> targetDeliverableIds = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.CampaignId == campaignId.Value && item.Id != source.CampaignDeliverableId)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);

            foreach (long targetId in targetDeliverableIds)
            {
                DeliverableContentLicense copy = new(targetId, source.Type, source.Channels, source.StartsAt, source.ExpiresAt, source.Value, source.Notes, source.CampaignDocumentId);
                dbContext.Set<DeliverableContentLicense>().Add(copy);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return targetDeliverableIds.Count;
        }

        public async Task<IReadOnlyList<ContentLicenseModel>> GetExpiring(int withinDays, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset limit = now.AddDays(withinDays);

            List<DeliverableContentLicense> licenses = await dbContext.Set<DeliverableContentLicense>()
                .AsNoTracking()
                .Where(item => item.ExpiresAt != null && item.ExpiresAt <= limit)
                .OrderBy(item => item.ExpiresAt)
                .ToListAsync(cancellationToken);

            return licenses.Select(item => ToModel(item, now)).ToList();
        }

        public async Task<int> AlertExpiring(IReadOnlyList<int> thresholdsDays, CancellationToken cancellationToken = default)
        {
            if (thresholdsDays.Count == 0)
            {
                return 0;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int maxThreshold = thresholdsDays.Max();
            DateTimeOffset limit = now.AddDays(maxThreshold);

            List<DeliverableContentLicense> licenses = await dbContext.Set<DeliverableContentLicense>()
                .AsTracking()
                .Where(item => item.ExpiresAt != null && item.ExpiresAt > now && item.ExpiresAt <= limit)
                .ToListAsync(cancellationToken);

            if (licenses.Count == 0)
            {
                return 0;
            }

            List<long> deliverableIds = licenses.Select(item => item.CampaignDeliverableId).Distinct().ToList();
            Dictionary<long, CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => deliverableIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            int alerted = 0;
            foreach (DeliverableContentLicense license in licenses)
            {
                int? days = license.DaysUntilExpiry(now);
                if (days is null)
                {
                    continue;
                }

                int applicable = thresholdsDays.Where(threshold => days.Value <= threshold).OrderBy(threshold => threshold).FirstOrDefault();
                if (applicable <= 0)
                {
                    continue;
                }

                if (license.LastAlertedThresholdDays is not null && applicable >= license.LastAlertedThresholdDays.Value)
                {
                    continue;
                }

                if (deliverables.TryGetValue(license.CampaignDeliverableId, out CampaignDeliverable? deliverable))
                {
                    await notificationService.Create(KanvasNotifications.ContentLicenseExpiring(deliverable, days.Value), cancellationToken);
                }

                license.MarkAlerted(applicable);
                alerted++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return alerted;
        }

        private async Task<DeliverableContentLicense> Load(long licenseId, CancellationToken cancellationToken)
        {
            DeliverableContentLicense? license = await dbContext.Set<DeliverableContentLicense>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == licenseId, cancellationToken);
            if (license is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return license;
        }

        private ContentLicenseModel ToModel(DeliverableContentLicense license, DateTimeOffset now)
        {
            return new ContentLicenseModel
            {
                Id = license.Id,
                DeliverableId = license.CampaignDeliverableId,
                Type = license.Type,
                Channels = license.Channels,
                StartsAt = license.StartsAt,
                ExpiresAt = license.ExpiresAt,
                Value = license.Value,
                Notes = license.Notes,
                CampaignDocumentId = license.CampaignDocumentId,
                Status = license.ComputeStatus(now, options.ExpiringSoonDays),
                DaysUntilExpiry = license.DaysUntilExpiry(now)
            };
        }
    }
}
