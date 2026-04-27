using AgencyCampaign.Application.Requests.IntegrationPipelines;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IIntegrationPipelineService : ICrudService<IntegrationPipeline>
    {
        Task<PagedResult<IntegrationPipeline>> GetPipelines(PagedRequest request, CancellationToken cancellationToken = default);

        Task<List<IntegrationPipeline>> GetActivePipelines(CancellationToken cancellationToken = default);

        Task<List<IntegrationPipeline>> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken = default);

        Task<IntegrationPipeline?> GetPipelineById(long id, CancellationToken cancellationToken = default);

        Task<IntegrationPipeline> CreatePipeline(CreateIntegrationPipelineRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationPipeline> UpdatePipeline(long id, UpdateIntegrationPipelineRequest request, CancellationToken cancellationToken = default);
    }
}
