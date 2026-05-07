using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class ChangeOpportunityStageRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CommercialPipelineStageId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
