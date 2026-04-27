using AgencyCampaign.Application.Requests.CampaignCreatorStatuses;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignCreatorStatusService : ICrudService<CampaignCreatorStatus>
    {
        Task<PagedResult<CampaignCreatorStatus>> GetStatuses(PagedRequest request, CancellationToken cancellationToken = default);

        Task<List<CampaignCreatorStatus>> GetActiveStatuses(CancellationToken cancellationToken = default);

        Task<CampaignCreatorStatus?> GetStatusById(long id, CancellationToken cancellationToken = default);

        Task<CampaignCreatorStatus> CreateStatus(CreateCampaignCreatorStatusRequest request, CancellationToken cancellationToken = default);

        Task<CampaignCreatorStatus> UpdateStatus(long id, UpdateCampaignCreatorStatusRequest request, CancellationToken cancellationToken = default);
    }
}
