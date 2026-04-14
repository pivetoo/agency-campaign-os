using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Platforms;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class PlatformService : CrudService<Platform>, IPlatformService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public PlatformService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Platform>> GetPlatforms(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Platform>()
                .AsNoTracking()
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Platform?> GetPlatformById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Platform>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<Platform>> GetActivePlatforms(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Platform>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Platform> CreatePlatform(CreatePlatformRequest request, CancellationToken cancellationToken = default)
        {
            Platform platform = new(request.Name, request.DisplayOrder);
            bool success = await Insert(cancellationToken, platform);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return platform;
        }

        public async Task<Platform> UpdatePlatform(long id, UpdatePlatformRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Platform? platform = await DbContext.Set<Platform>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (platform is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            platform.Update(request.Name, request.DisplayOrder, request.IsActive);

            Platform? result = await Update(platform, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
