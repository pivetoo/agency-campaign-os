using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class UpdateCreatorPaymentRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal GrossAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discounts { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
