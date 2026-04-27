using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IIntegrationLogService
    {
        Task<PagedResult<IntegrationLog>> GetLogs(PagedRequest request, CancellationToken cancellationToken = default);

        Task<List<IntegrationLog>> GetLogsByPipeline(long integrationPipelineId, CancellationToken cancellationToken = default);

        Task<IntegrationLog?> GetLogById(long id, CancellationToken cancellationToken = default);

        Task<IntegrationLog> CreateLog(long integrationPipelineId, int status, string? payload = null, string? response = null, long? durationMs = null, string? errorMessage = null, CancellationToken cancellationToken = default);
    }
}
