using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.IntegrationBindings;

namespace AgencyCampaign.Application.Services
{
    public interface IAgencyIntegrationBindingService
    {
        Task<IReadOnlyCollection<AgencyIntegrationBindingModel>> GetAll(CancellationToken cancellationToken = default);

        Task<AgencyIntegrationBindingModel?> GetByIntentKey(string intentKey, CancellationToken cancellationToken = default);

        Task<AgencyIntegrationBindingModel> Save(SaveAgencyIntegrationBindingRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteByIntentKey(string intentKey, CancellationToken cancellationToken = default);
    }
}
