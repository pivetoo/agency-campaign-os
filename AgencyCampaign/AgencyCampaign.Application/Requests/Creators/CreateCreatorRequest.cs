using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Creators
{
    public sealed class CreateCreatorRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(30)]
        public string? Document { get; set; }

        [StringLength(150)]
        public string? PixKey { get; set; }
    }
}
