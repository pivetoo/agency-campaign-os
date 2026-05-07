using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.AgencySettings;

namespace AgencyCampaign.Application.Services
{
    public interface IAgencySettingsService
    {
        Task<AgencySettingsModel> Get(CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> Update(UpdateAgencySettingsRequest request, CancellationToken cancellationToken = default);
    }
}
