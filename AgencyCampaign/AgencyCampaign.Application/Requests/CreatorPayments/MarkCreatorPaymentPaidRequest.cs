using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class MarkCreatorPaymentPaidRequest
    {
        [Required]
        public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;

        [StringLength(150)]
        public string? ProviderTransactionId { get; set; }

        [StringLength(50)]
        public string? Provider { get; set; }
    }
}
