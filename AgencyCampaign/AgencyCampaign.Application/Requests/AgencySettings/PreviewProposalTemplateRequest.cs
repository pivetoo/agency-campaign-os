using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.AgencySettings
{
    public sealed class PreviewProposalTemplateRequest
    {
        [Required]
        public string Template { get; set; } = string.Empty;
    }
}
