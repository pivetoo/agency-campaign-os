using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.CreatorSocialHandles;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorSocialHandleService
    {
        Task<IReadOnlyCollection<CreatorSocialHandleModel>> GetByCreator(long creatorId, CancellationToken cancellationToken = default);

        Task<CreatorSocialHandleModel> Create(CreateCreatorSocialHandleRequest request, CancellationToken cancellationToken = default);

        Task<CreatorSocialHandleModel> Update(long id, UpdateCreatorSocialHandleRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
