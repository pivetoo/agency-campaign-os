using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorService : CrudService<Creator>, ICreatorService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CreatorService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Creator>> GetCreators(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Creator>()
                .AsNoTracking()
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Creator?> GetCreatorById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Creator>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Creator> CreateCreator(CreateCreatorRequest request, CancellationToken cancellationToken = default)
        {
            Creator creator = new(request.Name, request.Email, request.Phone, request.Document, request.PixKey);
            bool success = await Insert(cancellationToken, creator);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return creator;
        }

        public async Task<Creator> UpdateCreator(long id, UpdateCreatorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Creator? creator = await DbContext.Set<Creator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (creator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            creator.Update(request.Name, request.Email, request.Phone, request.Document, request.PixKey, request.IsActive);

            Creator? result = await Update(creator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
