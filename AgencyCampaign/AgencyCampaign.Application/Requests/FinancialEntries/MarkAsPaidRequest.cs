using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    public sealed class MarkAsPaidRequest
    {
        public DateTimeOffset? PaidAt { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long AccountId { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }
    }
}
