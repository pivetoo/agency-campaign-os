using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AutomationDispatcher : IAutomationDispatcher
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([\w\.]+)\s*\}\}", RegexOptions.Compiled);

        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient client;
        private readonly ILogger<AutomationDispatcher> logger;

        public AutomationDispatcher(DbContext dbContext, IntegrationPlatformClient client, ILogger<AutomationDispatcher> logger)
        {
            this.dbContext = dbContext;
            this.client = client;
            this.logger = logger;
        }

        public async Task DispatchAsync(string trigger, IDictionary<string, object?> payload, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
            ArgumentNullException.ThrowIfNull(payload);

            List<Automation> automations = await dbContext.Set<Automation>()
                .AsNoTracking()
                .Where(item => item.IsActive && item.Trigger == trigger)
                .ToListAsync(cancellationToken);

            if (automations.Count == 0)
            {
                return;
            }

            foreach (Automation automation in automations)
            {
                string? renderedPayloadJson = null;
                try
                {
                    Dictionary<string, string> mapping = automation.GetVariableMapping();
                    Dictionary<string, object?> renderedPayload = mapping.ToDictionary(
                        entry => entry.Key,
                        entry => (object?)Render(entry.Value, payload));

                    renderedPayloadJson = JsonSerializer.Serialize(renderedPayload);

                    EnqueuePipelineRequest enqueueRequest = new()
                    {
                        ConnectorId = automation.ConnectorId,
                        PipelineId = automation.PipelineId,
                        Payload = renderedPayloadJson,
                        Priority = 0
                    };

                    await client.EnqueuePipelineAsync(enqueueRequest, cancellationToken);

                    logger.LogInformation("Automation '{Name}' dispatched for trigger '{Trigger}'.", automation.Name, trigger);
                    await PersistLogAsync(automation, trigger, renderedPayloadJson, succeeded: true, errorMessage: null, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to dispatch automation '{Name}' for trigger '{Trigger}'.", automation.Name, trigger);
                    await PersistLogAsync(automation, trigger, renderedPayloadJson, succeeded: false, errorMessage: exception.Message, cancellationToken);
                }
            }
        }

        private async Task PersistLogAsync(Automation automation, string trigger, string? renderedPayload, bool succeeded, string? errorMessage, CancellationToken cancellationToken)
        {
            try
            {
                AutomationExecutionLog log = new(automation.Id, automation.Name, trigger, succeeded, renderedPayload, errorMessage);
                dbContext.Set<AutomationExecutionLog>().Add(log);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to persist execution log for automation '{Name}'.", automation.Name);
            }
        }

        private static string Render(string template, IDictionary<string, object?> values)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return PlaceholderRegex.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (!values.TryGetValue(key, out object? value) || value is null)
                {
                    return string.Empty;
                }

                return value.ToString() ?? string.Empty;
            });
        }
    }
}
