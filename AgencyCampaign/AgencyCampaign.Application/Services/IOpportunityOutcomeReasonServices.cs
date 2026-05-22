using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityWinReasonService
    {
        Task<PagedResult<OpportunityWinReasonModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunityWinReasonModel> Create(CreateOpportunityWinReasonRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityWinReasonModel> Update(long id, UpdateOpportunityWinReasonRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }

    public interface IOpportunityLossReasonService
    {
        Task<PagedResult<OpportunityLossReasonModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunityLossReasonModel> Create(CreateOpportunityLossReasonRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityLossReasonModel> Update(long id, UpdateOpportunityLossReasonRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
