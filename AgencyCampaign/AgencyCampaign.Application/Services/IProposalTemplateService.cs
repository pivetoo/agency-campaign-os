using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IProposalTemplateService
    {
        Task<PagedResult<ProposalTemplateModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<ProposalTemplateModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<ProposalTemplateModel> Create(CreateProposalTemplateRequest request, CancellationToken cancellationToken = default);

        Task<ProposalTemplateModel> Update(long id, UpdateProposalTemplateRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);

        Task<int> ApplyToProposal(long proposalId, long templateId, CancellationToken cancellationToken = default);
    }

    public interface IProposalBlockService
    {
        Task<PagedResult<ProposalBlockModel>> GetAll(PagedRequest request, string? search, string? category, bool includeInactive, CancellationToken cancellationToken = default);

        Task<ProposalBlockModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<ProposalBlockModel> Create(CreateProposalBlockRequest request, CancellationToken cancellationToken = default);

        Task<ProposalBlockModel> Update(long id, UpdateProposalBlockRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
