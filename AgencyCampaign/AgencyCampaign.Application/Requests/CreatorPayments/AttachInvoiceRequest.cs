using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class AttachInvoiceRequest
    {
        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [StringLength(1000)]
        public string? InvoiceUrl { get; set; }

        public DateTimeOffset? IssuedAt { get; set; }
    }
}
