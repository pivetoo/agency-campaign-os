using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignFinancialEntries
{
    public sealed class UpdateCampaignFinancialEntryRequest
    {
        [Required]
        public long Id { get; set; }

        public long? CampaignDeliverableId { get; set; }

        [Required]
        public CampaignFinancialEntryType Type { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTimeOffset DueAt { get; set; }

        public DateTimeOffset? PaidAt { get; set; }

        [Required]
        public CampaignFinancialEntryStatus Status { get; set; } = CampaignFinancialEntryStatus.Pending;

        [StringLength(150)]
        public string? CounterpartyName { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
