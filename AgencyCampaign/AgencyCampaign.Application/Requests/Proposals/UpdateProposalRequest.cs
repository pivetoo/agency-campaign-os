using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class UpdateProposalRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue)]
        public long BrandId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTimeOffset? ValidityUntil { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}