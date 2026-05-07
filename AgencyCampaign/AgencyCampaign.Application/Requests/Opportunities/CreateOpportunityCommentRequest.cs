using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityCommentRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }

    public sealed class UpdateOpportunityCommentRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }
}
