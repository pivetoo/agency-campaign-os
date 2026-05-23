using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class AddOpportunityApprovalReviewerRequest
    {
        [Range(1, long.MaxValue)]
        public long? UserId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Role { get; set; }

        public bool Required { get; set; }
    }
}
