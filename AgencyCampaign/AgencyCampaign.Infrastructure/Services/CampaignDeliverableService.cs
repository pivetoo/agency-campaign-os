using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDeliverables;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDeliverableService : CrudService<CampaignDeliverable>, ICampaignDeliverableService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignDeliverableService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CampaignDeliverable>> GetDeliverables(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.DueAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignDeliverable?> GetDeliverableById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignDeliverable>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderBy(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignDeliverable> CreateDeliverable(CreateCampaignDeliverableRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CampaignCreatorId, cancellationToken);

            CampaignDeliverable deliverable = new(
                request.CampaignId,
                request.CampaignCreatorId,
                request.Title,
                request.Type,
                request.Platform,
                request.DueAt,
                request.GrossAmount,
                request.CreatorAmount,
                request.AgencyFeeAmount,
                request.Description,
                request.Notes);

            ApplyPublishing(deliverable, request.Status, request.PublishedUrl, request.EvidenceUrl);

            bool success = await Insert(cancellationToken, deliverable);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDeliverableById(deliverable.Id, cancellationToken) ?? deliverable;
        }

        public async Task<CampaignDeliverable> UpdateDeliverable(long id, UpdateCampaignDeliverableRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignDeliverable? deliverable = await DbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (deliverable is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            deliverable.Update(
                request.Title,
                request.Type,
                request.Platform,
                request.DueAt,
                request.GrossAmount,
                request.CreatorAmount,
                request.AgencyFeeAmount,
                request.Description,
                request.Notes);

            ApplyPublishing(deliverable, request.Status, request.PublishedUrl, request.EvidenceUrl);

            CampaignDeliverable? result = await Update(deliverable, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDeliverableById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<CampaignDeliverable?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            CampaignDeliverable? deliverable = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (deliverable is null)
            {
                MutableMessages.Add(new KeyNotFoundException(localizer["record.notFound"]));
                return null;
            }

            return await Delete([deliverable], cancellationToken) ? deliverable : null;
        }

        private async Task EnsureReferencesExist(long campaignId, long campaignCreatorId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool campaignCreatorExists = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignCreatorId && item.CampaignId == campaignId, cancellationToken);

            if (!campaignCreatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private void ApplyPublishing(CampaignDeliverable deliverable, DeliverableStatus status, string? publishedUrl, string? evidenceUrl)
        {
            if (status == DeliverableStatus.Published)
            {
                if (string.IsNullOrWhiteSpace(publishedUrl))
                {
                    throw new InvalidOperationException(localizer["deliverable.publishedUrl.required"]);
                }

                deliverable.Publish(publishedUrl, evidenceUrl, DateTimeOffset.UtcNow);
                return;
            }

            deliverable.ChangeStatus(status);
            deliverable.UpdateEvidence(evidenceUrl);
        }

        private IQueryable<CampaignDeliverable> QueryWithDetails()
        {
            return DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.Approvals);
        }
    }
}
