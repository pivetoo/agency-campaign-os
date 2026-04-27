using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignCreators
{
    public sealed class UpdateCampaignCreatorRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignCreatorStatusId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgreedAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
