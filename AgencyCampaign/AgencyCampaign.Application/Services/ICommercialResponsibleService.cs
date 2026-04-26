using AgencyCampaign.Application.Requests.CommercialResponsibles;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICommercialResponsibleService
    {
        Task<PagedResult<CommercialResponsible>> GetCommercialResponsibles(PagedRequest request, CancellationToken cancellationToken = default);

        Task<CommercialResponsible?> GetCommercialResponsibleById(long id, CancellationToken cancellationToken = default);

        Task<CommercialResponsible> CreateCommercialResponsible(CreateCommercialResponsibleRequest request, CancellationToken cancellationToken = default);

        Task<CommercialResponsible> UpdateCommercialResponsible(long id, UpdateCommercialResponsibleRequest request, CancellationToken cancellationToken = default);
    }
}
