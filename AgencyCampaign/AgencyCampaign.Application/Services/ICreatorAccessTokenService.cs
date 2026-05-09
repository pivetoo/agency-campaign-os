using AgencyCampaign.Application.Requests.CreatorAccessTokens;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorAccessTokenService : ICrudService<CreatorAccessToken>
    {
        Task<CreatorAccessToken> Issue(IssueCreatorAccessTokenRequest request, CancellationToken cancellationToken = default);
        Task<List<CreatorAccessToken>> GetByCreator(long creatorId, CancellationToken cancellationToken = default);
        Task<CreatorAccessToken?> ValidateToken(string token, CancellationToken cancellationToken = default);
        Task<bool> Revoke(long id, CancellationToken cancellationToken = default);
    }
}
