using AgencyCampaign.Application.Requests.Integrations;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IIntegrationService : ICrudService<Integration>
    {
        Task<PagedResult<Integration>> GetIntegrations(PagedRequest request, CancellationToken cancellationToken = default);

        Task<List<Integration>> GetActiveIntegrations(CancellationToken cancellationToken = default);

        Task<Integration?> GetIntegrationById(long id, CancellationToken cancellationToken = default);

        Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default);

        Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default);
    }
}
