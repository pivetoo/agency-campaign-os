using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityFollowUpRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long OpportunityId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset DueAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
