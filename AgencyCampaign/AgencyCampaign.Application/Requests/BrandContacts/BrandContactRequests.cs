using System.ComponentModel.DataAnnotations;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.BrandContacts
{
    public sealed class AddBrandContactRequest
    {
        [Required]
        public BrandContactType Type { get; set; }

        [Required]
        [StringLength(255)]
        public string Value { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Label { get; set; }
    }

    public sealed class UpdateBrandContactRequest
    {
        [Required]
        [StringLength(255)]
        public string Value { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Label { get; set; }
    }
}
