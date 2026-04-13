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

        [StringLength(150)]
        public string? TradeName { get; set; }

        [StringLength(30)]
        public string? Document { get; set; }

        [StringLength(100)]
        public string? ContactName { get; set; }

        [StringLength(150)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
