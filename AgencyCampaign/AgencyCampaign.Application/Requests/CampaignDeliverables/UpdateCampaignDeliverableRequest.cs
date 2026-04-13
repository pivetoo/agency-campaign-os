using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDeliverables
{
    public sealed class UpdateCampaignDeliverableRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTimeOffset DueAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        [Required]
        public DeliverableStatus Status { get; set; } = DeliverableStatus.Pending;

        [Range(0, double.MaxValue)]
        public decimal GrossAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CreatorAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgencyFeeAmount { get; set; }
    }
}
