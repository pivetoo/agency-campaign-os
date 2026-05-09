using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Campaigns
{
    public sealed class UpdateCampaignRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long BrandId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Objective { get; set; }

        [StringLength(4000)]
        public string? Briefing { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset? EndsAt { get; set; }

        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        [Range(1, long.MaxValue)]
        public long? ResponsibleUserId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
