using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreatorStatuses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignCreatorStatusService : CrudService<CampaignCreatorStatus>, ICampaignCreatorStatusService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignCreatorStatusService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CampaignCreatorStatus>> GetStatuses(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<CampaignCreatorStatus> query = DbContext.Set<CampaignCreatorStatus>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower));
            }

            return await query
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<CampaignCreatorStatus>> GetActiveStatuses(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignCreatorStatus>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignCreatorStatus?> GetStatusById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignCreatorStatus>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<CampaignCreatorStatus> CreateStatus(CreateCampaignCreatorStatusRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureInitialStageRules(request.IsInitial, null, cancellationToken);

            CampaignCreatorStatus status = new(
                request.Name,
                request.DisplayOrder,
                request.Color,
                request.Description,
                request.IsInitial,
                request.IsFinal,
                request.Category,
                request.MarksAsConfirmed);

            bool success = await Insert(cancellationToken, status);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return status;
        }

        public async Task<CampaignCreatorStatus> UpdateStatus(long id, UpdateCampaignCreatorStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            await EnsureInitialStageRules(request.IsInitial, id, cancellationToken);

            CampaignCreatorStatus? status = await DbContext.Set<CampaignCreatorStatus>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (status is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            status.Update(
                request.Name,
                request.DisplayOrder,
                request.Color,
                request.Description,
                request.IsInitial,
                request.IsFinal,
                request.Category,
                request.IsActive,
                request.MarksAsConfirmed);

            CampaignCreatorStatus? result = await Update(status, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureInitialStageRules(bool isInitial, long? excludeId, CancellationToken cancellationToken)
        {
            if (!isInitial)
            {
                return;
            }

            bool hasInitial = await DbContext.Set<CampaignCreatorStatus>()
                .AsNoTracking()
                .Where(item => item.IsInitial && (excludeId == null || item.Id != excludeId))
                .AnyAsync(cancellationToken);

            if (hasInitial)
            {
                throw new InvalidOperationException(localizer["campaignCreatorStatus.initial.duplicate"]);
            }
        }
    }
}
