using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocumentTemplates
{
    public sealed class PreviewCampaignDocumentTemplateRequest
    {
        [Required]
        public string Body { get; set; } = string.Empty;

        public CampaignDocumentType DocumentType { get; set; } = CampaignDocumentType.CreatorAgreement;
    }
}
