using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Campaigns
{
    public sealed class UpdateCampaignRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long BrandId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset? EndsAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
