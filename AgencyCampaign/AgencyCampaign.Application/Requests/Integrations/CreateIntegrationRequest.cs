using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Integrations
{
    public class CreateIntegrationRequest
    {
        [Required]
        [StringLength(120)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public long CategoryId { get; set; }
    }
}
