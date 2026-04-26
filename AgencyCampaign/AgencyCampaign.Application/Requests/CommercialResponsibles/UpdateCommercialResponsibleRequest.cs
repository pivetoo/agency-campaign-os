using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CommercialResponsibles
{
    public sealed class UpdateCommercialResponsibleRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}
