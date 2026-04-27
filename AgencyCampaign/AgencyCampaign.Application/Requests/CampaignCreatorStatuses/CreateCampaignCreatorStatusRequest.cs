using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignCreatorStatuses
{
    public class CreateCampaignCreatorStatusRequest
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";

        public bool IsInitial { get; set; }

        public bool IsFinal { get; set; }

        public CampaignCreatorStatusCategory Category { get; set; }
    }
}
