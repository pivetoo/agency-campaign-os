using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.IntegrationCapabilities
{
    public sealed class SetIntegrationCapabilityRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string IntentKey { get; set; } = string.Empty;

        [Required]
        public long ConnectorId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
