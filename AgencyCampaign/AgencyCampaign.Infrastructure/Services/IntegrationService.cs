using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Integrations;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationService : CrudService<Integration>, IIntegrationService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public IntegrationService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Integration>> GetIntegrations(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Integration>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<Integration>> GetActiveIntegrations(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Integration>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Integration?> GetIntegrationById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Integration>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            Integration integration = new(request.Identifier, request.Name, request.CategoryId, request.Description);
            bool success = await Insert(cancellationToken, integration);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return integration;
        }

        public async Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Integration? integration = await DbContext.Set<Integration>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            integration.Update(request.Identifier, request.Name, request.CategoryId, request.Description, request.IsActive);

            Integration? result = await Update(integration, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
