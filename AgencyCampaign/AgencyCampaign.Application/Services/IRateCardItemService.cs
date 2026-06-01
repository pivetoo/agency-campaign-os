using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;

namespace AgencyCampaign.Application.Services
{
    public interface IRateCardItemService
    {
        Task<IReadOnlyCollection<RateCardItemModel>> GetByCreator(long creatorId, bool includeInactive, CancellationToken cancellationToken = default);

        Task<RateCardItemModel> Create(CreateRateCardItemRequest request, CancellationToken cancellationToken = default);

        Task<RateCardItemModel> Update(long id, UpdateRateCardItemRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
