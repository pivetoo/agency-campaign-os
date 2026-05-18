using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.AgencySettings
{
    public sealed class UpdateProposalTemplateVersionRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string Template { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
