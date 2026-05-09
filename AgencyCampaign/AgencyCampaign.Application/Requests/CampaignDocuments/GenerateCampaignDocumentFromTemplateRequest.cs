using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class GenerateCampaignDocumentFromTemplateRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignId { get; set; }

        public long? CampaignCreatorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long TemplateId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        public Dictionary<string, string>? Overrides { get; set; }
    }
}
