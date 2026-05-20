using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Requests.IntegrationCapabilities;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface IIntegrationCapabilityService
    {
        Task<IReadOnlyList<IntegrationCapability>> GetAll(CancellationToken cancellationToken = default);

        Task<IntegrationCapability?> GetByIntent(string intentKey, CancellationToken cancellationToken = default);

        IReadOnlyList<IntegrationIntentDescriptor> GetIntentCatalog();

        Task<IntegrationCapability> SetCapability(SetIntegrationCapabilityRequest request, CancellationToken cancellationToken = default);

        Task<bool> RemoveCapability(string intentKey, CancellationToken cancellationToken = default);

        Task<ResolvedCapability> ResolveForExecution(string intentKey, CancellationToken cancellationToken = default);
    }

    public sealed record ResolvedCapability(string IntentKey, string ServiceContractIdentifier, long ConnectorId);
}
