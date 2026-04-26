using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long BrandId { get; set; }

        [Range(1, long.MaxValue)]
        public long? CommercialPipelineStageId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal EstimatedValue { get; set; }

        public DateTimeOffset? ExpectedCloseAt { get; set; }

        [Range(1, long.MaxValue)]
        public long? CommercialResponsibleId { get; set; }

        [StringLength(150)]
        public string? ContactName { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
