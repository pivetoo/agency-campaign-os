using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorAccessTokens
{
    public sealed class IssueCreatorAccessTokenRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CreatorId { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
