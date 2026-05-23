using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class AddOpportunityApprovalImpactRequest
    {
        [Required]
        [StringLength(60, MinimumLength = 2)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [StringLength(80, MinimumLength = 1)]
        public string Value { get; set; } = string.Empty;

        public bool IsGood { get; set; }

        public int DisplayOrder { get; set; }
    }
}
