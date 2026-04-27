using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.IntegrationPipelines;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationPipelineService : CrudService<IntegrationPipeline>, IIntegrationPipelineService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public IntegrationPipelineService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<IntegrationPipeline>> GetPipelines(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationPipeline>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<IntegrationPipeline>> GetActivePipelines(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationPipeline>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .Include(item => item.Integration)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<IntegrationPipeline>> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationPipeline>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IntegrationPipeline?> GetPipelineById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationPipeline>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<IntegrationPipeline> CreatePipeline(CreateIntegrationPipelineRequest request, CancellationToken cancellationToken = default)
        {
            IntegrationPipeline pipeline = new(request.IntegrationId, request.Identifier, request.Name, request.Description);
            bool success = await Insert(cancellationToken, pipeline);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return pipeline;
        }

        public async Task<IntegrationPipeline> UpdatePipeline(long id, UpdateIntegrationPipelineRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            IntegrationPipeline? pipeline = await DbContext.Set<IntegrationPipeline>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (pipeline is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            pipeline.Update(request.Identifier, request.Name, request.Description, request.IsActive);

            IntegrationPipeline? result = await Update(pipeline, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
