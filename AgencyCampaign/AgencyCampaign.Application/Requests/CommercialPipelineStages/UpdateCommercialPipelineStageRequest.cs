using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CommercialPipelineStages
{
    public sealed class UpdateCommercialPipelineStageRequest : CreateCommercialPipelineStageRequest
    {
        [Required]
        public long Id { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
