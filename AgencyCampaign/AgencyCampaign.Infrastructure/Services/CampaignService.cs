using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignService : CrudService<Campaign>, ICampaignService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Campaign>> GetCampaigns(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Campaign?> GetCampaignById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Campaign> CreateCampaign(CreateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureBrandExists(request.BrandId, cancellationToken);

            Campaign campaign = new(request.BrandId, request.Name, request.Budget, request.StartsAt, request.Description, request.EndsAt);
            bool success = await Insert(cancellationToken, campaign);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignById(campaign.Id, cancellationToken) ?? campaign;
        }

        public async Task<Campaign> UpdateCampaign(long id, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Campaign? campaign = await DbContext.Set<Campaign>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (campaign is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureBrandExists(request.BrandId, cancellationToken);

            campaign.Update(request.BrandId, request.Name, request.Budget, request.StartsAt, request.EndsAt, request.Description, request.IsActive);

            Campaign? result = await Update(campaign, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureBrandExists(long brandId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Brand>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == brandId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private IQueryable<Campaign> QueryWithDetails()
        {
            return DbContext.Set<Campaign>()
                .AsNoTracking()
                .Include(item => item.Brand);
        }
    }
}
