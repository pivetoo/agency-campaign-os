using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Brands
{
    public sealed class UpdateBrandRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactName { get; set; }

        [StringLength(150)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
