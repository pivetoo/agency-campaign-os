using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityApprovalCommentRequest
    {
        [Range(1, long.MaxValue)]
        public long? UserId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(40)]
        public string? Role { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }

    public sealed class UpdateOpportunityApprovalCommentRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }
}
