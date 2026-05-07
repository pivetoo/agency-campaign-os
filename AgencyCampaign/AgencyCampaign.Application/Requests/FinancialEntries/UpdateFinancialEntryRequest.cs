using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    public sealed class UpdateFinancialEntryRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long AccountId { get; set; }

        [Range(1, long.MaxValue)]
        public long? CampaignId { get; set; }

        public long? CampaignDeliverableId { get; set; }

        [Required]
        public FinancialEntryType Type { get; set; }

        [Required]
        public FinancialEntryCategory Category { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTimeOffset DueAt { get; set; }

        [Required]
        public DateTimeOffset OccurredAt { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        public string? ReferenceCode { get; set; }

        public DateTimeOffset? PaidAt { get; set; }

        [Required]
        public FinancialEntryStatus Status { get; set; } = FinancialEntryStatus.Pending;

        [StringLength(150)]
        public string? CounterpartyName { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
