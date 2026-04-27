using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.IntegrationPipelines
{
    public class CreateIntegrationPipelineRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long IntegrationId { get; set; }

        [Required]
        [StringLength(120)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
