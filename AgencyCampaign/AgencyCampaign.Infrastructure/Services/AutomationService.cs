using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Automations;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AutomationService : CrudService<Automation>, IAutomationService
    {

        public AutomationService(DbContext dbContext) : base(dbContext)
        {
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

        public async Task<Automation?> GetDefaultForUserAction(string intentKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return null;
            }

            string normalized = intentKey.Trim();
            return await DbContext.Set<Automation>()
                .AsNoTracking()
                .Where(item => item.TriggerType == AutomationTriggerType.UserAction
                    && item.Trigger == normalized
                    && item.IsActive)
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public IReadOnlyList<IntegrationIntentDescriptor> GetUserActionCatalog()
        {
            return IntegrationIntents.All;
        }

        public async Task<Automation> CreateAutomation(CreateAutomationRequest request, CancellationToken cancellationToken = default)
        {
            Automation automation = new(
                request.Name,
                request.Trigger,
                request.ConnectorId,
                request.PipelineId,
                request.TriggerCondition,
                request.VariableMapping,
                request.IsActive,
                request.TriggerType);

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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Automation? automation = await DbContext.Set<Automation>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (automation is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            automation.Update(
                request.Name,
                request.Trigger,
                request.ConnectorId,
                request.PipelineId,
                request.TriggerCondition,
                request.VariableMapping,
                request.IsActive,
                request.TriggerType);

            Automation? result = await Update(automation, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<PagedResult<AutomationExecutionLog>> GetExecutionLogs(long automationId, PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<AutomationExecutionLog>()
                .AsNoTracking()
                .Where(item => item.AutomationId == automationId)
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }
    }
}
