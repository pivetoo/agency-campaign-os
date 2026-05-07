using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CommercialResponsibles
{
    public sealed class CreateCommercialResponsibleRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long UserId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
