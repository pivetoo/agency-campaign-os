using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class SetOpportunityProbabilityRequest
    {
        [Required]
        [Range(0, 100)]
        public decimal Probability { get; set; }
    }
}
