using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignCreators
{
    public sealed class UpdateCampaignCreatorRequest
    {
        [Required]
        public long Id { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgreedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgencyFeeAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public CampaignCreatorStatus Status { get; set; }
    }
}
