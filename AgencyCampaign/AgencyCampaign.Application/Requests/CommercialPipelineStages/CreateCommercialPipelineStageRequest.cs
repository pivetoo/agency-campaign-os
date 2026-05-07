using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CommercialPipelineStages
{
    public class CreateCommercialPipelineStageRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";

        public bool IsInitial { get; set; }

        public bool IsFinal { get; set; }

        public CommercialPipelineStageFinalBehavior FinalBehavior { get; set; }

        [Range(0, 100)]
        public decimal? DefaultProbability { get; set; }

        [Range(1, int.MaxValue)]
        public int? SlaInDays { get; set; }
    }
}
