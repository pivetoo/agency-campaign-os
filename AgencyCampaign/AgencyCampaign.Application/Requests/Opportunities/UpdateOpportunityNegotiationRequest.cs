using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class UpdateOpportunityNegotiationRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTimeOffset NegotiatedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
