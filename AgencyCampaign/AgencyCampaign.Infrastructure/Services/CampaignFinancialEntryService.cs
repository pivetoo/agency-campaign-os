using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignFinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignFinancialEntryService : CrudService<CampaignFinancialEntry>, ICampaignFinancialEntryService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignFinancialEntryService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CampaignFinancialEntry>> GetEntries(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.DueAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignFinancialEntry?> GetEntryById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignFinancialEntry>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignFinancialEntry> CreateEntry(CreateCampaignFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CampaignDeliverableId, cancellationToken);

            CampaignFinancialEntry entry = new(
                request.CampaignId,
                request.Type,
                request.Category,
                request.Description,
                request.Amount,
                request.DueAt,
                request.OccurredAt,
                request.PaymentMethod,
                request.ReferenceCode,
                request.CounterpartyName,
                request.Notes,
                request.CampaignDeliverableId);

            entry.ChangeStatus(request.Status, request.PaidAt);

            bool success = await Insert(cancellationToken, entry);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetEntryById(entry.Id, cancellationToken) ?? entry;
        }

        public async Task<CampaignFinancialEntry> UpdateEntry(long id, UpdateCampaignFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignFinancialEntry? entry = await DbContext.Set<CampaignFinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureReferencesExist(entry.CampaignId, request.CampaignDeliverableId, cancellationToken);

            entry.Update(
                request.Type,
                request.Category,
                request.Description,
                request.Amount,
                request.DueAt,
                request.OccurredAt,
                request.PaymentMethod,
                request.ReferenceCode,
                request.CounterpartyName,
                request.Notes,
                request.CampaignDeliverableId);

            entry.ChangeStatus(request.Status, request.PaidAt);

            CampaignFinancialEntry? result = await Update(entry, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetEntryById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureReferencesExist(long campaignId, long? campaignDeliverableId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (!campaignDeliverableId.HasValue)
            {
                return;
            }

            bool deliverableExists = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignDeliverableId.Value && item.CampaignId == campaignId, cancellationToken);

            if (!deliverableExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private IQueryable<CampaignFinancialEntry> QueryWithDetails()
        {
            return DbContext.Set<CampaignFinancialEntry>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignDeliverable);
        }
    }
}
