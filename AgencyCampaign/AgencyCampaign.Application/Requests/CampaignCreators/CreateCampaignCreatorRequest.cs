using AgencyCampaign.Domain.ValueObjects;
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

        [Range(0, double.MaxValue)]
        public decimal AgreedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgencyFeeAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public CampaignCreatorStatus Status { get; set; } = CampaignCreatorStatus.Invited;
    }
}
