using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CommercialResponsibles
{
    public sealed class UpdateCommercialResponsibleRequest
    {
        [Required]
        public long Id { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}
