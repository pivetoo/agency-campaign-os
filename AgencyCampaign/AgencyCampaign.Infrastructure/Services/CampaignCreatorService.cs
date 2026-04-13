using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignCreatorService : CrudService<CampaignCreator>, ICampaignCreatorService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignCreatorService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CampaignCreator>> GetCampaignCreators(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignCreator?> GetCampaignCreatorById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignCreator>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignCreator> CreateCampaignCreator(CreateCampaignCreatorRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CreatorId, cancellationToken);
            await EnsureUniqueCreatorPerCampaign(request.CampaignId, request.CreatorId, cancellationToken);

            CampaignCreator campaignCreator = new(
                request.CampaignId,
                request.CreatorId,
                request.AgreedAmount,
                request.AgencyFeeAmount,
                request.Notes);

            campaignCreator.ChangeStatus(request.Status);

            bool success = await Insert(cancellationToken, campaignCreator);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignCreatorById(campaignCreator.Id, cancellationToken) ?? campaignCreator;
        }

        public async Task<CampaignCreator> UpdateCampaignCreator(long id, UpdateCampaignCreatorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignCreator? campaignCreator = await DbContext.Set<CampaignCreator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (campaignCreator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            campaignCreator.Update(request.AgreedAmount, request.AgencyFeeAmount, request.Notes);
            campaignCreator.ChangeStatus(request.Status);

            CampaignCreator? result = await Update(campaignCreator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignCreatorById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureReferencesExist(long campaignId, long creatorId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool creatorExists = await DbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == creatorId, cancellationToken);

            if (!creatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task EnsureUniqueCreatorPerCampaign(long campaignId, long creatorId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.CampaignId == campaignId && item.CreatorId == creatorId, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(localizer["campaignCreator.duplicate"]);
            }
        }

        private IQueryable<CampaignCreator> QueryWithDetails()
        {
            return DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.Creator);
        }
    }
}
