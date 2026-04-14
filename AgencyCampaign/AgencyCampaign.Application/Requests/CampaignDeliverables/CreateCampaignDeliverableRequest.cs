using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDeliverables
{
    public sealed class CreateCampaignDeliverableRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignCreatorId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long DeliverableKindId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PlatformId { get; set; }

        [Required]
        public DateTimeOffset DueAt { get; set; }

        [Required]
        public DeliverableStatus Status { get; set; } = DeliverableStatus.Pending;

        [StringLength(1000)]
        public string? PublishedUrl { get; set; }

        [StringLength(1000)]
        public string? EvidenceUrl { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal GrossAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CreatorAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AgencyFeeAmount { get; set; }
    }
}
