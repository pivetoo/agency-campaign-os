using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class CampaignDocumentProviderCallbackRequest
    {
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ProviderDocumentId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        public DateTimeOffset? OccurredAt { get; set; }

        [StringLength(150)]
        public string? SignerEmail { get; set; }

        [StringLength(150)]
        public string? ProviderSignerId { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(1000)]
        public string? SignedDocumentUrl { get; set; }

        public string? Metadata { get; set; }
    }
}
