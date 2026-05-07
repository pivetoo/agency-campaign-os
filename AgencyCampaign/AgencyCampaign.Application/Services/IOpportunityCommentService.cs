using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;

namespace AgencyCampaign.Application.Services
{
    public interface IOpportunityCommentService
    {
        Task<IReadOnlyCollection<OpportunityCommentModel>> GetByOpportunityId(long opportunityId, CancellationToken cancellationToken = default);

        Task<OpportunityCommentModel> CreateComment(long opportunityId, CreateOpportunityCommentRequest request, CancellationToken cancellationToken = default);

        Task<OpportunityCommentModel> UpdateComment(long commentId, UpdateOpportunityCommentRequest request, CancellationToken cancellationToken = default);

        Task DeleteComment(long commentId, CancellationToken cancellationToken = default);
    }
}
