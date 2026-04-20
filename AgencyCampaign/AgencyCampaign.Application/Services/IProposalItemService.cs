using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;

namespace AgencyCampaign.Application.Services
{
    public interface IProposalItemService : ICrudService<ProposalItem>
    {
        Task<ProposalItem?> GetProposalItemById(long id, CancellationToken cancellationToken = default);

        Task<ProposalItem> CreateProposalItem(CreateProposalItemRequest request, CancellationToken cancellationToken = default);

        Task<ProposalItem> UpdateProposalItem(long id, UpdateProposalItemRequest request, CancellationToken cancellationToken = default);

        Task DeleteProposalItem(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ProposalItem>> GetItemsByProposalId(long proposalId, CancellationToken cancellationToken = default);
    }
}