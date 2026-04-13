using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Campaigns
{
    public sealed class CreateCampaignRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
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
    }
}
