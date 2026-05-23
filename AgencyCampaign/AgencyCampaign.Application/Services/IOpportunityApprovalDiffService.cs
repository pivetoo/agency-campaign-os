using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityApprovalDiffService
    {
        Task<IReadOnlyCollection<OpportunityApprovalDiffModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalDiffModel> Add(long approvalId, AddOpportunityApprovalDiffRequest request, CancellationToken cancellationToken = default);

        Task Remove(long id, CancellationToken cancellationToken = default);
    }
}
