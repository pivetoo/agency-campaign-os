using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class CreatorPaymentProviderCallbackRequest
    {
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ProviderTransactionId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        [StringLength(140)]
        public string? EndToEndId { get; set; }

        public DateTimeOffset? OccurredAt { get; set; }

        [StringLength(1000)]
        public string? FailureReason { get; set; }

        public string? Metadata { get; set; }
    }
}
