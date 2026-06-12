using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.ContentLicenses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Options;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
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

        // Listagem paginada de todas as licencas (tela de Producao). Filtros traduziveis em SQL (status por
        // faixa de data, tipo, campanha, busca) para funcionar tanto no Postgres quanto no EF InMemory dos testes.
        public async Task<PagedResult<ContentLicenseModel>> GetLicenses(PagedRequest request, ContentLicenseStatus? status, ContentLicenseType? type, long? campaignId, string? search, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset soonLimit = now.AddDays(options.ExpiringSoonDays);

            var query = from license in dbContext.Set<DeliverableContentLicense>().AsNoTracking()
                        join deliverable in dbContext.Set<CampaignDeliverable>().AsNoTracking() on license.CampaignDeliverableId equals deliverable.Id
                        join campaign in dbContext.Set<Campaign>().AsNoTracking() on deliverable.CampaignId equals campaign.Id
                        join campaignCreator in dbContext.Set<CampaignCreator>().AsNoTracking() on deliverable.CampaignCreatorId equals campaignCreator.Id
                        join creator in dbContext.Set<Creator>().AsNoTracking() on campaignCreator.CreatorId equals creator.Id
                        select new LicenseRow
                        {
                            License = license,
                            DeliverableTitle = deliverable.Title,
                            CampaignId = campaign.Id,
                            CampaignName = campaign.Name,
                            CreatorName = creator.StageName ?? creator.Name
                        };

            if (type.HasValue)
            {
                query = query.Where(row => row.License.Type == type.Value);
            }

            if (campaignId.HasValue)
            {
                query = query.Where(row => row.CampaignId == campaignId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(row =>
                    row.DeliverableTitle.ToLower().Contains(term) ||
                    row.CampaignName.ToLower().Contains(term) ||
                    row.CreatorName.ToLower().Contains(term));
            }

            query = status switch
            {
                ContentLicenseStatus.Active => query.Where(row => row.License.ExpiresAt == null || row.License.ExpiresAt > soonLimit),
                ContentLicenseStatus.ExpiringSoon => query.Where(row => row.License.ExpiresAt != null && row.License.ExpiresAt > now && row.License.ExpiresAt <= soonLimit),
                ContentLicenseStatus.Expired => query.Where(row => row.License.ExpiresAt != null && row.License.ExpiresAt <= now),
                _ => query
            };

            query = query
                .OrderBy(row => row.License.ExpiresAt == null)
                .ThenBy(row => row.License.ExpiresAt)
                .ThenByDescending(row => row.License.Id);

            return await query.ToPagedResultAsync(request, row => ToModel(row.License, now, row.CampaignId, row.DeliverableTitle, row.CampaignName, row.CreatorName), cancellationToken);
        }

        public async Task<IReadOnlyList<ContentLicenseModel>> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default)
        {
            List<DeliverableContentLicense> licenses = await dbContext.Set<DeliverableContentLicense>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == deliverableId)
                .OrderBy(item => item.ExpiresAt)
                .ToListAsync(cancellationToken);

            var info = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.Id == deliverableId)
                .Select(item => new { item.CampaignId, item.Title })
                .FirstOrDefaultAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            return licenses.Select(item => ToModel(item, now, info?.CampaignId ?? 0, info?.Title)).ToList();
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

            List<long> deliverableIds = licenses.Select(item => item.CampaignDeliverableId).Distinct().ToList();
            var infoRows = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => deliverableIds.Contains(item.Id))
                .Select(item => new { item.Id, item.CampaignId, item.Title })
                .ToListAsync(cancellationToken);
            Dictionary<long, (long CampaignId, string Title)> infos = infoRows.ToDictionary(item => item.Id, item => (item.CampaignId, item.Title));

            return licenses.Select(item =>
            {
                infos.TryGetValue(item.CampaignDeliverableId, out (long CampaignId, string Title) info);
                return ToModel(item, now, info.CampaignId, info.Title);
            }).ToList();
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

        private ContentLicenseModel ToModel(DeliverableContentLicense license, DateTimeOffset now, long campaignId = 0, string? deliverableTitle = null, string? campaignName = null, string? creatorName = null)
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
                DaysUntilExpiry = license.DaysUntilExpiry(now),
                CampaignId = campaignId,
                DeliverableTitle = deliverableTitle,
                CampaignName = campaignName,
                CreatorName = creatorName
            };
        }

        private sealed class LicenseRow
        {
            public DeliverableContentLicense License { get; init; } = null!;
            public string DeliverableTitle { get; init; } = string.Empty;
            public long CampaignId { get; init; }
            public string CampaignName { get; init; } = string.Empty;
            public string CreatorName { get; init; } = string.Empty;
        }
    }
}
