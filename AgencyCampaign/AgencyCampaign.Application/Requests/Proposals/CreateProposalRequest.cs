using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class CreateProposalRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long BrandId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTimeOffset? ValidityUntil { get; set; }

        [Range(1, long.MaxValue)]
        public long? OpportunityId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public long? InternalOwnerId { get; set; }

        public string? InternalOwnerName { get; set; }
    }
}
