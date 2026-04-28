using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Automations;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AutomationService : CrudService<Automation>, IAutomationService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public AutomationService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Automation>> GetAutomations(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Automation>()
                .AsNoTracking()
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Automation?> GetAutomationById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Automation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Automation> CreateAutomation(CreateAutomationRequest request, CancellationToken cancellationToken = default)
        {
            Automation automation = new(
                request.Name,
                request.Trigger,
                request.ConnectorId,
                request.PipelineId,
                request.TriggerCondition,
                request.VariableMapping)
            {
                IsActive = request.IsActive
            };

            bool success = await Insert(cancellationToken, automation);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return automation;
        }

        public async Task<Automation> UpdateAutomation(long id, UpdateAutomationRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Automation? automation = await DbContext.Set<Automation>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (automation is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            automation.Update(
                request.Name,
                request.Trigger,
                request.ConnectorId,
                request.PipelineId,
                request.TriggerCondition,
                request.VariableMapping,
                request.IsActive);

            Automation? result = await Update(automation, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
