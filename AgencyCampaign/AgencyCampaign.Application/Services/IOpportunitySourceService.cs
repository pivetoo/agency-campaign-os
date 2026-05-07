using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunitySourceService
    {
        Task<IReadOnlyCollection<OpportunitySourceModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunitySourceModel> Create(CreateOpportunitySourceRequest request, CancellationToken cancellationToken = default);

        Task<OpportunitySourceModel> Update(long id, UpdateOpportunitySourceRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }

    public interface IOpportunityTagService
    {
        Task<IReadOnlyCollection<OpportunityTagModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default);

        Task<OpportunityTagModel> Create(CreateOpportunityTagRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityTagModel> Update(long id, UpdateOpportunityTagRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
