using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityApprovalRequestService : ICrudService<OpportunityApprovalRequest>
    {
        Task<OpportunityApprovalRequest?> GetOpportunityApprovalRequestById(long id, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalRequest> CreateOpportunityApprovalRequest(CreateOpportunityApprovalRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalRequest> Approve(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityApprovalRequest> Reject(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OpportunityApprovalRequest>> GetApprovalsByNegotiationId(long opportunityNegotiationId, CancellationToken cancellationToken = default);

        Task<PagedResult<OpportunityApprovalRequest>> GetAllApprovals(PagedRequest request, CancellationToken cancellationToken = default);

        Task<ApprovalSummaryModel> GetApprovalsSummary(CancellationToken cancellationToken = default);
    }
}
