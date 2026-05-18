using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableKinds;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DeliverableKindService : CrudService<DeliverableKind>, IDeliverableKindService
    {

        public DeliverableKindService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<DeliverableKind>> GetDeliverableKinds(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<DeliverableKind> query = DbContext.Set<DeliverableKind>().AsNoTracking();
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

        public async Task<DeliverableKind?> GetDeliverableKindById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<DeliverableKind>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<DeliverableKind>> GetActiveDeliverableKinds(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<DeliverableKind>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<DeliverableKind> CreateDeliverableKind(CreateDeliverableKindRequest request, CancellationToken cancellationToken = default)
        {
            DeliverableKind deliverableKind = new(request.Name, request.DisplayOrder);
            bool success = await Insert(cancellationToken, deliverableKind);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return deliverableKind;
        }

        public async Task<DeliverableKind> UpdateDeliverableKind(long id, UpdateDeliverableKindRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            DeliverableKind? deliverableKind = await DbContext.Set<DeliverableKind>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (deliverableKind is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            deliverableKind.Update(request.Name, request.DisplayOrder, request.IsActive);

            DeliverableKind? result = await Update(deliverableKind, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
