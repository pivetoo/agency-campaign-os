using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IProposalService : ICrudService<Proposal>
    {
        Task<PagedResult<Proposal>> GetProposals(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Proposal?> GetProposalById(long id, CancellationToken cancellationToken = default);

        Task<Proposal> CreateProposal(CreateProposalRequest request, CancellationToken cancellationToken = default);

        Task<Proposal> UpdateProposal(long id, UpdateProposalRequest request, CancellationToken cancellationToken = default);

        Task<Proposal> MarkAsSent(long id, CancellationToken cancellationToken = default);

        Task<Proposal> MarkAsViewed(long id, CancellationToken cancellationToken = default);

        Task<Proposal> ApproveProposal(long id, CancellationToken cancellationToken = default);

        Task<Proposal> RejectProposal(long id, CancellationToken cancellationToken = default);

        Task<Proposal> ConvertToCampaign(long id, long campaignId, CancellationToken cancellationToken = default);

        Task<Proposal> CancelProposal(long id, CancellationToken cancellationToken = default);
    }
}