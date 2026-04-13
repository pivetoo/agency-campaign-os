using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Creators
{
    public sealed class UpdateCreatorRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? StageName { get; set; }

        [StringLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(30)]
        public string? Document { get; set; }

        [StringLength(150)]
        public string? PixKey { get; set; }

        [StringLength(120)]
        public string? PrimaryNiche { get; set; }

        [StringLength(120)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
