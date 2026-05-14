using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunitySourceService
    {
        Task<PagedResult<OpportunitySourceModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunitySourceModel> Create(CreateOpportunitySourceRequest request, CancellationToken cancellationToken = default);

        Task<OpportunitySourceModel> Update(long id, UpdateOpportunitySourceRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }

    public interface IOpportunityTagService
    {
        Task<PagedResult<OpportunityTagModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunityTagModel> Create(CreateOpportunityTagRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityTagModel> Update(long id, UpdateOpportunityTagRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
