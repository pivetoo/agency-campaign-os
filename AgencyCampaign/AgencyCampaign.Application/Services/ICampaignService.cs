using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignService : ICrudService<Campaign>
    {
        Task<PagedResult<Campaign>> GetCampaigns(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Campaign?> GetCampaignById(long id, CancellationToken cancellationToken = default);

        Task<Campaign> CreateCampaign(CreateCampaignRequest request, CancellationToken cancellationToken = default);

        Task<Campaign> UpdateCampaign(long id, UpdateCampaignRequest request, CancellationToken cancellationToken = default);

        Task<CampaignSummaryModel?> GetSummary(long id, CancellationToken cancellationToken = default);
    }
}
