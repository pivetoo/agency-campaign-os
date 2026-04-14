using AgencyCampaign.Application.Requests.DeliverableKinds;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IDeliverableKindService : ICrudService<DeliverableKind>
    {
        Task<PagedResult<DeliverableKind>> GetDeliverableKinds(PagedRequest request, CancellationToken cancellationToken = default);

        Task<DeliverableKind?> GetDeliverableKindById(long id, CancellationToken cancellationToken = default);

        Task<List<DeliverableKind>> GetActiveDeliverableKinds(CancellationToken cancellationToken = default);

        Task<DeliverableKind> CreateDeliverableKind(CreateDeliverableKindRequest request, CancellationToken cancellationToken = default);

        Task<DeliverableKind> UpdateDeliverableKind(long id, UpdateDeliverableKindRequest request, CancellationToken cancellationToken = default);
    }
}
