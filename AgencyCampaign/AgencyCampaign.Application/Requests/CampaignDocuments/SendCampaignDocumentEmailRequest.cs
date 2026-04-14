using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class SendCampaignDocumentEmailRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(5000)]
        public string? Body { get; set; }
    }
}
