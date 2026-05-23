using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityApprovalImpactService
    {
        Task<IReadOnlyCollection<OpportunityApprovalImpactModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalImpactModel> Add(long approvalId, AddOpportunityApprovalImpactRequest request, CancellationToken cancellationToken = default);

        Task Remove(long id, CancellationToken cancellationToken = default);
    }
}
