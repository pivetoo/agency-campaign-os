using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.AgencySettings
{
    public sealed class SaveProposalTemplateVersionRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Template { get; set; } = string.Empty;

        public bool Activate { get; set; } = true;
    }
}
