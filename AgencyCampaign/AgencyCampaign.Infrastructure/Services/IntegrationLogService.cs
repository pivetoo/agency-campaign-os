using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationLogService : CrudService<IntegrationLog>, IIntegrationLogService
    {
        public IntegrationLogService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<IntegrationLog>> GetLogs(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationLog>()
                .AsNoTracking()
                .Include(item => item.IntegrationPipeline)
                .ThenInclude(item => item!.Integration)
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<IntegrationLog>> GetLogsByPipeline(long integrationPipelineId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationLog>()
                .AsNoTracking()
                .Where(item => item.IntegrationPipelineId == integrationPipelineId)
                .Include(item => item.IntegrationPipeline)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IntegrationLog?> GetLogById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationLog>()
                .AsNoTracking()
                .Include(item => item.IntegrationPipeline)
                .ThenInclude(item => item!.Integration)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<IntegrationLog> CreateLog(long integrationPipelineId, int status, string? payload = null, string? response = null, long? durationMs = null, string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            IntegrationLog log = new(integrationPipelineId, status, payload, response, durationMs, errorMessage);
            bool success = await Insert(cancellationToken, log);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return log;
        }
    }
}
