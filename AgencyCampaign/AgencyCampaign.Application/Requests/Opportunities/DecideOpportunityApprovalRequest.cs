using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class DecideOpportunityApprovalRequest
    {
        [Range(1, long.MaxValue)]
        public long? ApprovedByUserId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string ApprovedByUserName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? DecisionNotes { get; set; }
    }
}
