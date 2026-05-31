using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Commercial
{
    public sealed class UpsertCommercialPolicyRequest
    {
        [Range(0, 100)]
        public decimal? MaxDiscountPercent { get; set; }

        [Range(0, 3650)]
        public int? DefaultPaymentTermDays { get; set; }

        [Range(0, 3650)]
        public int? MaxPaymentTermDays { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
