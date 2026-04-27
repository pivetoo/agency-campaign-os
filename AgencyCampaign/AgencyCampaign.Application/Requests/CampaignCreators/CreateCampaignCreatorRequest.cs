using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignCreators
{
    public sealed class CreateCampaignCreatorRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long CreatorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignCreatorStatusId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgreedAmount { get; set; }

        [Range(0, 100)]
        public decimal AgencyFeePercent { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
