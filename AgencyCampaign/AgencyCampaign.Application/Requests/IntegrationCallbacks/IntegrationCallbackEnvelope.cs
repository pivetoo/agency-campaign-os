using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AgencyCampaign.Application.Requests.IntegrationCallbacks
{
    public sealed class IntegrationCallbackEnvelope
    {
        [Required]
        public string ServiceIdentifier { get; set; } = string.Empty;

        public long ConnectorId { get; set; }

        public string? ConnectorName { get; set; }

        public long ExecutionId { get; set; }

        public string? Status { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? FinishedAt { get; set; }

        public JsonElement? Output { get; set; }
    }
}
