using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityService : ICrudService<Opportunity>
    {
        Task<PagedResult<Opportunity>> GetOpportunities(PagedRequest request, OpportunityListFilters filters, CancellationToken cancellationToken = default);

        Task<Opportunity?> GetOpportunityById(long id, CancellationToken cancellationToken = default);

        Task<Opportunity> CreateOpportunity(CreateOpportunityRequest request, CancellationToken cancellationToken = default);

        Task<Opportunity> UpdateOpportunity(long id, UpdateOpportunityRequest request, CancellationToken cancellationToken = default);

        Task<Opportunity> ChangeStage(long id, ChangeOpportunityStageRequest request, CancellationToken cancellationToken = default);

        Task<Opportunity> CloseAsWon(long id, CloseOpportunityAsWonRequest request, CancellationToken cancellationToken = default);

        Task<Opportunity> CloseAsLost(long id, CloseOpportunityAsLostRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OpportunityBoardStageModel>> GetBoard(CancellationToken cancellationToken = default);

        Task<CommercialDashboardSummaryModel> GetDashboardSummary(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CommercialAlertModel>> GetAlerts(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OpportunityStageHistoryModel>> GetStageHistory(long opportunityId, CancellationToken cancellationToken = default);
    }
}
