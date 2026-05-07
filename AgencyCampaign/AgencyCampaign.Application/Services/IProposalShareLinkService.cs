using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;

namespace AgencyCampaign.Application.Services
{
    public interface IProposalShareLinkService
    {
        Task<IReadOnlyCollection<ProposalShareLinkModel>> GetByProposalId(long proposalId, CancellationToken cancellationToken = default);

        Task<ProposalShareLinkModel> CreateShareLink(long proposalId, CreateProposalShareLinkRequest request, CancellationToken cancellationToken = default);

        Task<ProposalShareLinkModel> RevokeShareLink(long shareLinkId, CancellationToken cancellationToken = default);
    }

    public interface IProposalVersionService
    {
        Task<IReadOnlyCollection<ProposalVersionModel>> GetByProposalId(long proposalId, CancellationToken cancellationToken = default);

        Task<ProposalVersionDetailModel?> GetById(long versionId, CancellationToken cancellationToken = default);
    }

    public interface IProposalPublicService
    {
        Task<ProposalPublicViewModel?> GetByToken(string token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    }
}
