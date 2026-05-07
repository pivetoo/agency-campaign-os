using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorSocialHandles
{
    public class CreateCreatorSocialHandleRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CreatorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PlatformId { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Handle { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProfileUrl { get; set; }

        [Range(0, long.MaxValue)]
        public long? Followers { get; set; }

        [Range(0, 100)]
        public decimal? EngagementRate { get; set; }

        public bool IsPrimary { get; set; }
    }

    public sealed class UpdateCreatorSocialHandleRequest : CreateCreatorSocialHandleRequest
    {
        [Required]
        public long Id { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
