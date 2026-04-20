using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityNegotiationService : ICrudService<OpportunityNegotiation>
    {
        Task<OpportunityNegotiation?> GetOpportunityNegotiationById(long id, CancellationToken cancellationToken = default);

        Task<OpportunityNegotiation> CreateOpportunityNegotiation(CreateOpportunityNegotiationRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityNegotiation> UpdateOpportunityNegotiation(long id, UpdateOpportunityNegotiationRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityNegotiation> ChangeStatus(long id, ChangeOpportunityNegotiationStatusRequest request, CancellationToken cancellationToken = default);

        Task DeleteOpportunityNegotiation(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OpportunityNegotiation>> GetNegotiationsByOpportunityId(long opportunityId, CancellationToken cancellationToken = default);
    }
}
