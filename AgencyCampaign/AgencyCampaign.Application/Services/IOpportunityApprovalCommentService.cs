using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityApprovalCommentService
    {
        Task<IReadOnlyCollection<OpportunityApprovalCommentModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalCommentModel> Create(long approvalId, CreateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalCommentModel> Update(long id, UpdateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
