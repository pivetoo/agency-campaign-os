using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class ResubmitOpportunityApprovalRequest
    {
        [Range(1, long.MaxValue)]
        public long? RequestedByUserId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string RequestedByUserName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Reason { get; set; }
    }
}
