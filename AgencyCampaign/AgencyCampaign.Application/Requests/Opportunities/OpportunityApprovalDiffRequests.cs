using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class AddOpportunityApprovalDiffRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Field { get; set; } = string.Empty;

        [StringLength(200)]
        public string PolicyValue { get; set; } = string.Empty;

        [StringLength(200)]
        public string RequestedValue { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Delta { get; set; }

        [Range(1, 3)]
        public int Kind { get; set; } = 1;

        public int DisplayOrder { get; set; }
    }
}
