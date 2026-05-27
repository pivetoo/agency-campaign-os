using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class UpdateProposalRequest
    {
        [Required]
        public long Id { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTimeOffset? ValidityUntil { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long OpportunityId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public long? ProposalLayoutId { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        [Range(0, 3650)]
        public int? PaymentTermDays { get; set; }
    }
}
