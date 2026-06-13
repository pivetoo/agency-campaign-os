using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    // Webhook do provedor de cobranca, ecoado pelo pipeline do IntegrationPlatform. ChargeId correlaciona
    // os callbacks seguintes; FinancialEntryId e ecoado do payload de emissao para o primeiro callback
    // (ou em qualquer ordem). Eventos: created/issued, paid, expired, cancelled, failed.
    public sealed class FinancialEntryChargeCallbackRequest
    {
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ChargeId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        public long? FinancialEntryId { get; set; }

        [StringLength(1000)]
        public string? ChargeUrl { get; set; }

        public DateTimeOffset? OccurredAt { get; set; }

        public DateTimeOffset? PaidAt { get; set; }

        [StringLength(140)]
        public string? EndToEndId { get; set; }

        public string? Metadata { get; set; }
    }
}
