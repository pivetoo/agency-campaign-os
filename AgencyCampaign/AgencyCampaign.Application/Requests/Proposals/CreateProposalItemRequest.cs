using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class CreateProposalItemRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long ProposalId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 2)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public DateTimeOffset? DeliveryDeadline { get; set; }

        [Range(1, long.MaxValue)]
        public long? CreatorId { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }
    }
}