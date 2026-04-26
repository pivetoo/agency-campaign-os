using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialResponsibles;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialResponsibleService : CrudService<CommercialResponsible>, ICommercialResponsibleService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CommercialResponsibleService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CommercialResponsible>> GetCommercialResponsibles(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CommercialResponsible?> GetCommercialResponsibleById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<CommercialResponsible> CreateCommercialResponsible(CreateCommercialResponsibleRequest request, CancellationToken cancellationToken = default)
        {
            CommercialResponsible responsible = new(
                request.Name,
                request.Email,
                request.Phone,
                request.Notes);

            bool success = await Insert(cancellationToken, responsible);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return responsible;
        }

        public async Task<CommercialResponsible> UpdateCommercialResponsible(long id, UpdateCommercialResponsibleRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CommercialResponsible? responsible = await DbContext.Set<CommercialResponsible>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (responsible is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            responsible.Update(request.Name, request.Email, request.Phone, request.Notes, request.IsActive);

            CommercialResponsible? result = await Update(responsible, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
