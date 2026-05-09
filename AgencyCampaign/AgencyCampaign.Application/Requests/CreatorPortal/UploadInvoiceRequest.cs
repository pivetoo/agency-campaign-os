using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPortal
{
    public sealed class UploadInvoiceRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CreatorPaymentId { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string InvoiceUrl { get; set; } = string.Empty;

        public DateTimeOffset? IssuedAt { get; set; }
    }
}
