using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocumentTemplates
{
    public sealed class CreateCampaignDocumentTemplateRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public CampaignDocumentType DocumentType { get; set; }

        [Required]
        [MinLength(10)]
        public string Body { get; set; } = string.Empty;
    }
}
