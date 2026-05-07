using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.DeliverableShareLinks
{
    public sealed class CreateDeliverableShareLinkRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignDeliverableId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string ReviewerName { get; set; } = string.Empty;

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public sealed class PublicDeliverableDecisionRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string ReviewerName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Comment { get; set; }
    }
}
