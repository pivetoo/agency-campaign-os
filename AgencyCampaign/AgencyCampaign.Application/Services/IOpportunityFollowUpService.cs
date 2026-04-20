using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityFollowUpService : ICrudService<OpportunityFollowUp>
    {
        Task<OpportunityFollowUp?> GetOpportunityFollowUpById(long id, CancellationToken cancellationToken = default);

        Task<OpportunityFollowUp> CreateOpportunityFollowUp(CreateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityFollowUp> UpdateOpportunityFollowUp(long id, UpdateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityFollowUp> CompleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default);

        Task DeleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OpportunityFollowUp>> GetFollowUpsByOpportunityId(long opportunityId, CancellationToken cancellationToken = default);
    }
}
