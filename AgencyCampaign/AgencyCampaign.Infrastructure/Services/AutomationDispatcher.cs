using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Clients;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AutomationDispatcher : IAutomationDispatcher
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([\w\.]+)\s*\}\}", RegexOptions.Compiled);

        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient client;

        public AutomationDispatcher(DbContext dbContext, IntegrationPlatformClient client)
        {
            this.dbContext = dbContext;
            this.client = client;
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
                try
                {
                    Dictionary<string, string> mapping = automation.GetVariableMapping();
                    Dictionary<string, object?> renderedPayload = mapping.ToDictionary(
                        entry => entry.Key,
                        entry => (object?)Render(entry.Value, payload));

                    EnqueuePipelineRequest enqueueRequest = new()
                    {
                        ConnectorId = automation.ConnectorId,
                        PipelineId = automation.PipelineId,
                        Payload = JsonSerializer.Serialize(renderedPayload),
                        Priority = 0
                    };

                    await client.EnqueuePipelineAsync(enqueueRequest, cancellationToken);
                    Console.WriteLine($"[AutomationDispatcher] dispatched '{automation.Name}' for trigger '{trigger}'.");
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"[AutomationDispatcher] failed to dispatch '{automation.Name}' for trigger '{trigger}': {exception.Message}");
                }
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
