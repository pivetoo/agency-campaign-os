using AgencyCampaign.Application.Requests.CampaignCreators;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignCreatorService : ICrudService<CampaignCreator>
    {
        Task<PagedResult<CampaignCreator>> GetCampaignCreators(PagedRequest request, CancellationToken cancellationToken = default);

        Task<CampaignCreator?> GetCampaignCreatorById(long id, CancellationToken cancellationToken = default);

        Task<List<CampaignCreator>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);

        Task<CampaignCreator> CreateCampaignCreator(CreateCampaignCreatorRequest request, CancellationToken cancellationToken = default);

        Task<CampaignCreator> UpdateCampaignCreator(long id, UpdateCampaignCreatorRequest request, CancellationToken cancellationToken = default);
    }
}
