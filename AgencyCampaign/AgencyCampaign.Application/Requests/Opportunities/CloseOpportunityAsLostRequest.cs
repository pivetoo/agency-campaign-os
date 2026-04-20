using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CloseOpportunityAsLostRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string LossReason { get; set; } = string.Empty;
    }
}
