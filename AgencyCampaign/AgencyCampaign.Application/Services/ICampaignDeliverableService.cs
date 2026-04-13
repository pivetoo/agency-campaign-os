using AgencyCampaign.Application.Requests.CampaignDeliverables;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignDeliverableService : ICrudService<CampaignDeliverable>
    {
        Task<PagedResult<CampaignDeliverable>> GetDeliverables(PagedRequest request, CancellationToken cancellationToken = default);

        Task<CampaignDeliverable?> GetDeliverableById(long id, CancellationToken cancellationToken = default);

        Task<List<CampaignDeliverable>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);

        Task<CampaignDeliverable> CreateDeliverable(CreateCampaignDeliverableRequest request, CancellationToken cancellationToken = default);

        Task<CampaignDeliverable> UpdateDeliverable(long id, UpdateCampaignDeliverableRequest request, CancellationToken cancellationToken = default);
    }
}
