using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class CreateCampaignDocumentRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignId { get; set; }

        public long? CampaignCreatorId { get; set; }

        [Required]
        public CampaignDocumentType DocumentType { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? DocumentUrl { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
