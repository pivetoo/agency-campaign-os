using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Automations
{
    public sealed class CreateAutomationRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Trigger { get; set; } = string.Empty;

        [StringLength(500)]
        public string? TriggerCondition { get; set; }

        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public Dictionary<string, string> VariableMapping { get; set; } = [];

        public bool IsActive { get; set; } = true;
    }
}
