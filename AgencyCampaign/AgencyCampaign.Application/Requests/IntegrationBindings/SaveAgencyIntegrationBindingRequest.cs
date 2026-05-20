using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.IntegrationBindings
{
    public sealed class SaveAgencyIntegrationBindingRequest
    {
        [Required]
        [StringLength(80, MinimumLength = 1)]
        public string IntentKey { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
