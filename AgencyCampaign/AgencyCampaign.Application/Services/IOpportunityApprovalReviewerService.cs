using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityApprovalReviewerService
    {
        Task<IReadOnlyCollection<OpportunityApprovalReviewerModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalReviewerModel> Add(long approvalId, AddOpportunityApprovalReviewerRequest request, CancellationToken cancellationToken = default);

        Task Remove(long id, CancellationToken cancellationToken = default);
    }
}
