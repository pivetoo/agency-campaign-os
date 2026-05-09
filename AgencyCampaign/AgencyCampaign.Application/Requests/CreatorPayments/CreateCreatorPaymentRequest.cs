using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class CreateCreatorPaymentRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignCreatorId { get; set; }

        public long? CampaignDocumentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal GrossAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discounts { get; set; }

        [Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Pix;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
